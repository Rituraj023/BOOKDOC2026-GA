using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Queues;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Queues;

namespace BookDoc2026.Application.Queues;

public sealed class QueueService(
    IQueueRepository repository,
    IQueueRealtimeNotifier realtime,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlationContext)
{
    public async Task<ImagingServicePointResponse> CreateServicePointAsync(
        long branchId,
        CreateImagingServicePointRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.QueuesServicePointsManage, branchId, cancellationToken);
        if (!Enum.TryParse<ImagingModality>(request.Modality, true, out var modality))
            throw new DomainRuleException("Imaging modality must be XRay or CT.");
        var resourceId = publicIds.DecodeOptional(PublicIdKind.BookableResource, request.ResourceId, branch.TenantId);
        if (resourceId.HasValue && !await repository.ResourceExistsAsync(branchId, resourceId.Value, cancellationToken))
            throw new NotFoundException("The service-point resource was not found in this branch.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await repository.ServicePointCodeExistsAsync(branchId, code, cancellationToken))
            throw new DomainRuleException("An imaging service point already uses this code.");
        var now = clock.UtcNow;
        var servicePoint = ImagingServicePoint.Create(
            branch.TenantId, branchId, code, request.Name, modality, resourceId, now);
        var audit = AuditEvent.Record(
            branch.TenantId, branchId, actor.ActorId, "QueueServicePoint.Created",
            nameof(ImagingServicePoint), servicePoint.Id,
            JsonSerializer.Serialize(new { servicePoint.Code, servicePoint.Modality, servicePoint.ResourceId }),
            correlationContext.CorrelationId, now);
        await repository.AddServicePointAsync(servicePoint, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(servicePoint);
    }

    public async Task<IReadOnlyCollection<ImagingServicePointResponse>> ListServicePointsAsync(
        long branchId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchWithAnyPermissionAsync(branchId, cancellationToken,
            FoundationPermissions.QueuesView,
            FoundationPermissions.QueuesCheckIn,
            FoundationPermissions.QueuesServicePointsManage);
        return (await repository.ListServicePointsAsync(branchId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<QueueTicketResponse> CheckInAsync(
        long branchId,
        CheckInQueueTicketRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.QueuesCheckIn, branchId, cancellationToken);
        var servicePointId = publicIds.Decode(PublicIdKind.ImagingServicePoint, request.ServicePointId, branch.TenantId);
        var patientId = publicIds.Decode(PublicIdKind.Patient, request.PatientId, branch.TenantId);
        var bookingId = publicIds.DecodeOptional(PublicIdKind.Booking, request.BookingId, branch.TenantId);
        var servicePoint = await repository.GetServicePointAsync(branchId, servicePointId, cancellationToken)
            ?? throw new NotFoundException("Imaging service point was not found.");
        if (!servicePoint.IsActive) throw new DomainRuleException("The imaging service point is inactive.");
        if (!await repository.PatientExistsAsync(patientId, cancellationToken))
            throw new NotFoundException("Patient was not found.");
        if (bookingId.HasValue && !await repository.BookingMatchesAsync(branchId, bookingId.Value, patientId, cancellationToken))
            throw new DomainRuleException("The Booking does not belong to this patient and branch.");
        if (!Enum.TryParse<QueuePriority>(request.Priority, true, out var priority))
            throw new DomainRuleException("Queue priority is invalid.");
        if (priority == QueuePriority.Urgent)
        {
            if (!actor.HasPermission(FoundationPermissions.QueuesPriorityManage))
                throw new ForbiddenException("Urgent queue priority requires additional permission.");
            _ = QueueTicket.NormalizeReason(request.PriorityReason ?? string.Empty);
        }

        var now = clock.UtcNow;
        var ticket = QueueTicket.CheckIn(
            branch.TenantId, branchId, servicePointId, patientId, bookingId, request.RequestId, priority, now);
        var queueEvent = QueueTicketEvent.Record(ticket, null, "CheckedIn", actor.ActorId,
            priority == QueuePriority.Urgent ? request.PriorityReason : null, now);
        var audit = AuditEvent.Record(
            branch.TenantId, branchId, actor.ActorId, "QueueTicket.CheckedIn",
            nameof(QueueTicket), ticket.Id,
            JsonSerializer.Serialize(new { servicePointId, bookingId, ticket.Priority, ticket.DisplayToken }),
            correlationContext.CorrelationId, now);
        var result = await repository.CreateTicketAsync(ticket, queueEvent, audit, cancellationToken);
        if (!result.IsReplay) await NotifyAsync(result.Ticket, now, cancellationToken);
        return Map(result.Ticket, result.IsReplay);
    }

    public async Task<IReadOnlyCollection<QueueTicketResponse>> ListAsync(
        long branchId,
        long servicePointId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.QueuesView, branchId, cancellationToken);
        _ = await repository.GetServicePointAsync(branchId, servicePointId, cancellationToken)
            ?? throw new NotFoundException("Imaging service point was not found.");
        return (await repository.ListTicketsAsync(branchId, servicePointId, cancellationToken))
            .Select(ticket => Map(ticket, false)).ToArray();
    }

    public async Task<IReadOnlyCollection<QueueDisplayTicketResponse>> DisplayAsync(
        long branchId,
        long servicePointId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.QueuesDisplayView, branchId, cancellationToken);
        _ = await repository.GetServicePointAsync(branchId, servicePointId, cancellationToken)
            ?? throw new NotFoundException("Imaging service point was not found.");
        return (await repository.ListTicketsAsync(branchId, servicePointId, cancellationToken))
            .Where(ticket => ticket.Status is QueueTicketStatus.Called or QueueTicketStatus.Preparation or QueueTicketStatus.InService)
            .Select(ticket => new QueueDisplayTicketResponse(
                ticket.DisplayToken, ticket.Status.ToString(), ticket.CallCount, ticket.Version, ticket.ModifiedUtc))
            .ToArray();
    }

    public Task<QueueTicketResponse> CallAsync(long branchId, long ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, ticketId, request, FoundationPermissions.QueuesCall, "Called",
            (ticket, now) => ticket.Call(request.ExpectedVersion, now), cancellationToken);

    public Task<QueueTicketResponse> RecallAsync(long branchId, long ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, ticketId, request, FoundationPermissions.QueuesCall, "Recalled",
            (ticket, now) => ticket.Recall(request.ExpectedVersion, request.Reason ?? string.Empty, now), cancellationToken);

    public Task<QueueTicketResponse> PrepareAsync(long branchId, long ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, ticketId, request, FoundationPermissions.QueuesProgress, "PreparationStarted",
            (ticket, now) => ticket.BeginPreparation(request.ExpectedVersion, now), cancellationToken);

    public Task<QueueTicketResponse> StartAsync(long branchId, long ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, ticketId, request, FoundationPermissions.QueuesProgress, "ImagingStarted",
            (ticket, now) => ticket.StartService(request.ExpectedVersion, now), cancellationToken);

    public Task<QueueTicketResponse> CompleteAsync(long branchId, long ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, ticketId, request, FoundationPermissions.QueuesProgress, "Completed",
            (ticket, now) => ticket.Complete(request.ExpectedVersion, now), cancellationToken);

    public Task<QueueTicketResponse> CancelAsync(long branchId, long ticketId, QueueTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, ticketId, request, FoundationPermissions.QueuesCancel, "Cancelled",
            (ticket, now) => ticket.Cancel(request.ExpectedVersion, request.Reason ?? string.Empty, now), cancellationToken);

    private async Task<QueueTicketResponse> TransitionAsync(
        long branchId,
        long ticketId,
        QueueTransitionRequest request,
        string permission,
        string action,
        Action<QueueTicket, DateTimeOffset> transition,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(permission, branchId, cancellationToken);
        var ticket = await repository.GetTicketAsync(branchId, ticketId, true, cancellationToken)
            ?? throw new NotFoundException("Queue ticket was not found.");
        var from = ticket.Status;
        var now = clock.UtcNow;
        transition(ticket, now);
        var queueEvent = QueueTicketEvent.Record(ticket, from, action, actor.ActorId, request.Reason, now);
        var audit = AuditEvent.Record(
            branch.TenantId, branchId, actor.ActorId, $"QueueTicket.{action}", nameof(QueueTicket), ticket.Id,
            JsonSerializer.Serialize(new { From = from, To = ticket.Status, ticket.Version }),
            correlationContext.CorrelationId, now);
        await repository.AddTransitionEvidenceAsync(queueEvent, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await NotifyAsync(ticket, now, cancellationToken);
        return Map(ticket, false);
    }

    private async Task<Branch> RequireBranchAsync(string permission, long branchId, CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this queue operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private async Task<Branch> RequireBranchWithAnyPermissionAsync(
        long branchId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !permissions.Any(actor.HasPermission))
            throw new ForbiddenException("The actor is not authorized for this queue operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private Task NotifyAsync(QueueTicket ticket, DateTimeOffset now, CancellationToken cancellationToken) =>
        realtime.NotifyAsync(new(
            ticket.TenantId, ticket.BranchId, ticket.ServicePointId, ticket.Id,
            ticket.Status, ticket.Version, now), cancellationToken);

    private ImagingServicePointResponse Map(ImagingServicePoint item) => new(
        publicIds.Encode(PublicIdKind.ImagingServicePoint, item.Id, item.TenantId),
        item.Code, item.Name, item.Modality.ToString(),
        publicIds.EncodeOptional(PublicIdKind.BookableResource, item.ResourceId, item.TenantId),
        item.IsActive, item.Version);

    private QueueTicketResponse Map(QueueTicket ticket, bool isReplay) => new(
        publicIds.Encode(PublicIdKind.QueueTicket, ticket.Id, ticket.TenantId),
        publicIds.Encode(PublicIdKind.ImagingServicePoint, ticket.ServicePointId, ticket.TenantId),
        publicIds.Encode(PublicIdKind.Patient, ticket.PatientId, ticket.TenantId),
        publicIds.EncodeOptional(PublicIdKind.Booking, ticket.BookingId, ticket.TenantId),
        ticket.DisplayToken, ticket.Priority.ToString(), ticket.Status.ToString(), ticket.ArrivedUtc,
        ticket.CalledUtc, ticket.ServiceStartedUtc, ticket.CompletedUtc, ticket.CancelledUtc,
        ticket.CallCount, ticket.Version, isReplay);
}
