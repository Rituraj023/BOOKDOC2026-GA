using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Queues;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Queues;

namespace BookDoc2026.Application.Clinical;

public sealed class InvestigationService(
    IInvestigationRepository repository,
    IQueueRealtimeNotifier realtime,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlation)
{
    public async Task<InvestigationOrderResponse> CreateAsync(
        long branchId, long encounterId, CreateInvestigationOrderRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.InvestigationOrdersCreate,
            branchId, cancellationToken);
        var encounter = await repository.GetEncounterAsync(branchId, encounterId, cancellationToken)
            ?? throw new NotFoundException("Encounter was not found.");
        var serviceId = publicIds.Decode(PublicIdKind.ClinicalService, request.ServiceId, branch.TenantId);
        if (!Enum.TryParse<ImagingModality>(request.Modality, true, out var modality))
            throw new DomainRuleException("Investigation modality must be XRay or CT.");
        var requestedService = await repository.GetInvestigationServiceAsync(serviceId, modality, cancellationToken)
            ?? throw new DomainRuleException(
                "The selected catalog service is not configured with a matching required X-ray or CT resource category.");

        var now = clock.UtcNow;
        var order = InvestigationOrder.Request(encounter, requestedService.Service.Id, modality,
            request.ClinicalIndication, request.RequestId, actor.ActorId, now);
        var orderEvent = InvestigationOrderEvent.Record(order, "Requested", actor.ActorId, null, now);
        var audit = AuditEvent.Record(branch.TenantId, branchId, actor.ActorId,
            "InvestigationOrder.Requested", nameof(InvestigationOrder), order.Id,
            JsonSerializer.Serialize(new { encounterId, serviceId, Modality = modality.ToString() }),
            correlation.CorrelationId, now);
        var result = await repository.CreateOrderAsync(order, orderEvent, audit, cancellationToken);
        return Map(result.Aggregate, result.IsReplay);
    }

    public async Task<IReadOnlyCollection<InvestigationServiceOptionResponse>> ListCatalogOptionsAsync(
        long branchId, long encounterId, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.InvestigationOrdersCreate,
            branchId, cancellationToken);
        var encounter = await repository.GetEncounterAsync(branchId, encounterId, cancellationToken)
            ?? throw new NotFoundException("Encounter was not found.");
        if (encounter.Status != EncounterStatus.Signed)
            throw new DomainRuleException("A signed encounter is required to select an investigation service.");

        return (await repository.ListInvestigationServicesAsync(cancellationToken))
            .Select(item => new InvestigationServiceOptionResponse(
                publicIds.Encode(PublicIdKind.ClinicalService, item.Service.Id, branch.TenantId),
                item.Service.Code, item.Service.Name, item.Modality.ToString()))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<InvestigationOrderResponse>> ListAsync(
        long branchId, long encounterId, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.InvestigationsView, branchId, cancellationToken);
        _ = await repository.GetEncounterAsync(branchId, encounterId, cancellationToken)
            ?? throw new NotFoundException("Encounter was not found.");
        return (await repository.ListOrdersAsync(branchId, encounterId, cancellationToken))
            .Select(item => Map(item, false)).ToArray();
    }

    public async Task<IReadOnlyCollection<InvestigationWorklistItemResponse>> ListWorklistAsync(
        long branchId, long servicePointId, int take, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.InvestigationWorklistView,
            branchId, cancellationToken);
        if (take is < 1 or > 200)
            throw new DomainRuleException("Radiology worklist size must be between 1 and 200.");
        var servicePoint = await repository.GetServicePointAsync(branchId, servicePointId, cancellationToken)
            ?? throw new NotFoundException("Imaging service point was not found.");

        return (await repository.ListWorklistAsync(branchId, servicePointId, take, cancellationToken))
            .Where(item => item.Order.Modality == servicePoint.Modality)
            .Select(MapWorklistItem)
            .ToArray();
    }

    public async Task<InvestigationOrderResponse> HandoffToQueueAsync(
        long branchId, long encounterId, long orderId, HandoffInvestigationOrderRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.InvestigationQueueHandoff,
            branchId, cancellationToken);
        if (!actor.HasPermission(FoundationPermissions.QueuesCheckIn))
            throw new ForbiddenException("Queue check-in permission is also required for an investigation handoff.");
        var aggregate = await repository.GetOrderAsync(branchId, encounterId, orderId, true, cancellationToken)
            ?? throw new NotFoundException("Investigation order was not found.");
        var servicePointId = publicIds.Decode(PublicIdKind.ImagingServicePoint,
            request.ServicePointId, branch.TenantId);
        var servicePoint = await repository.GetServicePointAsync(branchId, servicePointId, cancellationToken)
            ?? throw new NotFoundException("Imaging service point was not found.");
        if (!servicePoint.IsActive) throw new DomainRuleException("The imaging service point is inactive.");
        if (servicePoint.Modality != aggregate.Order.Modality)
            throw new DomainRuleException("The service-point modality does not match the investigation order.");
        if (!Enum.TryParse<QueuePriority>(request.Priority, true, out var priority))
            throw new DomainRuleException("Queue priority is invalid.");
        if (priority == QueuePriority.Urgent)
        {
            if (!actor.HasPermission(FoundationPermissions.QueuesPriorityManage))
                throw new ForbiddenException("Urgent queue priority requires additional permission.");
            _ = QueueTicket.NormalizeReason(request.PriorityReason ?? string.Empty);
        }

        if (aggregate.QueueTicket is not null)
        {
            ValidateReplay(aggregate.QueueTicket, request.RequestId, servicePointId, priority);
            return Map(aggregate, true);
        }

        var now = clock.UtcNow;
        var ticket = QueueTicket.CheckInInvestigation(branch.TenantId, branchId, servicePointId,
            aggregate.Order.PatientId, aggregate.Order.Id, request.RequestId, priority, now);
        aggregate.Order.RecordQueueHandoff(request.ExpectedVersion, actor.ActorId, now);
        var queueEvent = QueueTicketEvent.Record(ticket, null, "CheckedIn", actor.ActorId,
            priority == QueuePriority.Urgent ? request.PriorityReason : null, now);
        var orderEvent = InvestigationOrderEvent.Record(aggregate.Order, "QueueLinked", actor.ActorId,
            ticket.Id, now);
        var orderAudit = AuditEvent.Record(branch.TenantId, branchId, actor.ActorId,
            "InvestigationOrder.QueueLinked", nameof(InvestigationOrder), aggregate.Order.Id,
            JsonSerializer.Serialize(new { ticketId = ticket.Id, servicePointId, ticket.Priority }),
            correlation.CorrelationId, now);
        var queueAudit = AuditEvent.Record(branch.TenantId, branchId, actor.ActorId,
            "QueueTicket.CheckedIn", nameof(QueueTicket), ticket.Id,
            JsonSerializer.Serialize(new { servicePointId, investigationOrderId = aggregate.Order.Id,
                ticket.Priority, ticket.DisplayToken }), correlation.CorrelationId, now);
        var result = await repository.CreateQueueHandoffAsync(aggregate.Order, ticket, queueEvent,
            orderEvent, orderAudit, queueAudit, cancellationToken);
        if (!result.IsReplay)
            await realtime.NotifyAsync(new(ticket.TenantId, ticket.BranchId, ticket.ServicePointId,
                ticket.Id, ticket.Status, ticket.Version, now), cancellationToken);
        return Map(result.Aggregate, result.IsReplay);
    }

    private async Task<Branch> RequireBranchAsync(string permission, long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this investigation operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private static void ValidateReplay(QueueTicket ticket, Guid requestId, long servicePointId,
        QueuePriority priority)
    {
        if (ticket.RequestId != requestId || ticket.ServicePointId != servicePointId || ticket.Priority != priority)
            throw new DomainRuleException("This investigation order already has a different queue handoff.");
    }

    private InvestigationOrderResponse Map(InvestigationOrderAggregate aggregate, bool isReplay)
    {
        var order = aggregate.Order;
        return new(
            publicIds.Encode(PublicIdKind.InvestigationOrder, order.Id, order.TenantId),
            publicIds.Encode(PublicIdKind.Encounter, order.EncounterId, order.TenantId),
            publicIds.Encode(PublicIdKind.Patient, order.PatientId, order.TenantId),
            publicIds.Encode(PublicIdKind.ClinicalService, order.RequestedServiceId, order.TenantId),
            aggregate.Service.Code, aggregate.Service.Name, order.OrderNumber, order.Modality.ToString(),
            order.ClinicalIndication, order.Status.ToString(), order.ResultStatus.ToString(),
            publicIds.Encode(PublicIdKind.IdentitySubject, order.RequestedByActorId), order.OrderedUtc,
            order.QueueHandoffUtc,
            order.QueueHandoffByActorId.HasValue
                ? publicIds.Encode(PublicIdKind.IdentitySubject, order.QueueHandoffByActorId.Value)
                : null,
            order.Version,
            aggregate.QueueTicket is null ? null : Map(aggregate.QueueTicket, isReplay),
            aggregate.History.Select(item => new InvestigationOrderEventResponse(
                publicIds.Encode(PublicIdKind.InvestigationOrderEvent, item.Id, item.TenantId),
                item.Action, publicIds.Encode(PublicIdKind.IdentitySubject, item.ActorId),
                publicIds.EncodeOptional(PublicIdKind.QueueTicket, item.QueueTicketId, item.TenantId),
                item.OrderVersion, item.CreatedUtc)).ToArray(),
            isReplay);
    }

    private InvestigationWorklistItemResponse MapWorklistItem(InvestigationWorklistProjection item) => new(
        Map(item.QueueTicket, false),
        publicIds.Encode(PublicIdKind.InvestigationOrder, item.Order.Id, item.Order.TenantId),
        item.Order.OrderNumber,
        publicIds.Encode(PublicIdKind.ClinicalService, item.Service.Id, item.Order.TenantId),
        item.Service.Code,
        item.Service.Name,
        item.Order.Modality.ToString(),
        item.Order.ClinicalIndication,
        item.Order.OrderedUtc,
        publicIds.Encode(PublicIdKind.Patient, item.Patient.Id, item.Order.TenantId),
        item.Patient.PatientNumber,
        item.PatientStakeholder.DisplayName,
        publicIds.Encode(PublicIdKind.Encounter, item.Encounter.Id, item.Order.TenantId),
        item.Encounter.EncounterNumber);

    private QueueTicketResponse Map(QueueTicket ticket, bool isReplay) => new(
        publicIds.Encode(PublicIdKind.QueueTicket, ticket.Id, ticket.TenantId),
        publicIds.Encode(PublicIdKind.ImagingServicePoint, ticket.ServicePointId, ticket.TenantId),
        publicIds.Encode(PublicIdKind.Patient, ticket.PatientId, ticket.TenantId),
        publicIds.EncodeOptional(PublicIdKind.Booking, ticket.BookingId, ticket.TenantId),
        publicIds.EncodeOptional(PublicIdKind.InvestigationOrder, ticket.InvestigationOrderId, ticket.TenantId),
        ticket.DisplayToken, ticket.Priority.ToString(), ticket.Status.ToString(), ticket.ArrivedUtc,
        ticket.CalledUtc, ticket.ServiceStartedUtc, ticket.CompletedUtc, ticket.CancelledUtc,
        ticket.CallCount, ticket.Version, isReplay);
}
