using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Scheduling;

public sealed class SchedulingService(
    ISchedulingRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlationContext)
{
    public async Task<AvailabilityRuleResponse> CreateRuleAsync(long branchId, CreateAvailabilityRuleRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingAvailabilityManage, branchId, cancellationToken);
        var resourceId = publicIds.Decode(PublicIdKind.BookableResource, request.ResourceId, branch.TenantId);
        var serviceId = publicIds.DecodeOptional(PublicIdKind.ClinicalService, request.ServiceId, branch.TenantId);
        var resource = await repository.GetResourceAsync(branchId, resourceId, cancellationToken)
            ?? throw new NotFoundException("Resource was not found in this branch.");
        if (serviceId.HasValue && !await repository.ResourceSupportsServiceAsync(resource.Id, serviceId.Value, cancellationToken))
            throw new DomainRuleException("The resource does not support the selected service.");
        if (!Enum.TryParse<DayOfWeek>(request.DayOfWeek, true, out var day))
            throw new DomainRuleException("Day of week is invalid.");
        var rule = AvailabilityRule.Create(branch.TenantId, branchId, resource.Id, serviceId, day,
            request.LocalStart, request.LocalEnd, request.EffectiveFrom, request.EffectiveTo,
            request.SlotIntervalMinutes, request.Capacity, clock.UtcNow);
        await repository.AddRuleAsync(rule, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task<AvailabilityExceptionResponse> CreateExceptionAsync(long branchId, CreateAvailabilityExceptionRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingAvailabilityManage, branchId, cancellationToken);
        var resourceId = publicIds.Decode(PublicIdKind.BookableResource, request.ResourceId, branch.TenantId);
        _ = await repository.GetResourceAsync(branchId, resourceId, cancellationToken)
            ?? throw new NotFoundException("Resource was not found in this branch.");
        if (!Enum.TryParse<AvailabilityExceptionKind>(request.Kind, true, out var kind))
            throw new DomainRuleException("Availability exception kind is invalid.");
        var item = AvailabilityException.Create(branch.TenantId, branchId, resourceId,
            request.StartUtc, request.EndUtc, kind, request.CapacityOverride, request.Reason, clock.UtcNow);
        await repository.AddExceptionAsync(item, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<IReadOnlyCollection<AvailabilityResourceResponse>> SearchAvailabilityAsync(
        long branchId, long serviceId, DateTimeOffset startUtc, DateTimeOffset endUtc, int quantity,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingAvailabilityView, branchId, cancellationToken);
        if (startUtc >= endUtc || startUtc <= clock.UtcNow || quantity is < 1 or > 1000)
            throw new DomainRuleException("Availability search interval or quantity is invalid.");
        if (!await repository.ServiceExistsAsync(serviceId, cancellationToken))
            throw new NotFoundException("Service was not found.");

        var resources = await repository.ListServiceResourcesAsync(branchId, serviceId, cancellationToken);
        var ids = resources.Select(resource => resource.Id).ToArray();
        var rules = await repository.ListRulesAsync(branchId, ids, cancellationToken);
        var exceptions = await repository.ListExceptionsAsync(branchId, ids, startUtc, endUtc, cancellationToken);
        var reserved = await repository.GetReservedQuantitiesAsync(ids, startUtc, endUtc, clock.UtcNow, cancellationToken);
        return resources.Select(resource => Evaluate(resource, serviceId, startUtc, endUtc, quantity, rules, exceptions,
            reserved.GetValueOrDefault(resource.Id))).ToArray();
    }

    public async Task<SchedulingHoldResponse> CreateHoldAsync(long branchId, CreateSchedulingHoldRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingHoldsCreate, branchId, cancellationToken);
        var patientId = publicIds.Decode(PublicIdKind.Patient, request.PatientId, branch.TenantId);
        var serviceId = publicIds.Decode(PublicIdKind.ClinicalService, request.ServiceId, branch.TenantId);
        var resources = request.Resources.Select(item => new
        {
            ResourceId = publicIds.Decode(PublicIdKind.BookableResource, item.ResourceId, branch.TenantId),
            item.Quantity,
            item.RequirementRoleCode
        }).ToArray();
        if (!await repository.PatientExistsAsync(patientId, cancellationToken)) throw new NotFoundException("Patient was not found.");
        if (!await repository.ServiceExistsAsync(serviceId, cancellationToken)) throw new NotFoundException("Service was not found.");
        if (resources.Length == 0 || resources.Select(item => item.ResourceId).Distinct().Count() != resources.Length)
            throw new DomainRuleException("At least one unique resource is required for a hold.");

        var now = clock.UtcNow;
        var canonical = JsonSerializer.Serialize(new
        {
            PatientId = patientId,
            ServiceId = serviceId,
            request.StartUtc,
            request.EndUtc,
            request.HoldMinutes,
            Resources = resources.OrderBy(item => item.ResourceId).Select(item => new
            {
                item.ResourceId,
                item.Quantity,
                RequirementRoleCode = string.IsNullOrWhiteSpace(item.RequirementRoleCode)
                    ? null
                    : item.RequirementRoleCode.Trim().ToUpperInvariant()
            })
        });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var hold = SchedulingHold.Create(branch.TenantId, branchId, patientId, serviceId,
            request.RequestId, hash, request.StartUtc, request.EndUtc, now.AddMinutes(request.HoldMinutes), now);
        var reservations = resources.Select(item => ResourceReservation.Create(branch.TenantId, branchId,
            hold.Id, item.ResourceId, request.StartUtc, request.EndUtc, item.Quantity, now,
            item.RequirementRoleCode)).ToArray();
        var audit = AuditEvent.Record(branch.TenantId, branchId, actor.ActorId, "SchedulingHold.Created",
            nameof(SchedulingHold), hold.Id, JsonSerializer.Serialize(new { PatientId = patientId, ServiceId = serviceId, ResourceCount = reservations.Length }),
            correlationContext.CorrelationId, now);
        var result = await repository.CreateHoldAtomicallyAsync(hold, reservations, audit, now, cancellationToken);
        return Map(result.Aggregate);
    }

    public async Task<SchedulingHoldResponse> GetHoldAsync(long branchId, long holdId, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingAvailabilityView, branchId, cancellationToken);
        var aggregate = await repository.GetHoldAsync(branchId, holdId, cancellationToken)
            ?? throw new NotFoundException("Scheduling hold was not found.");
        aggregate.Hold.MarkExpired(clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    public async Task<SchedulingHoldResponse> ReleaseHoldAsync(long branchId, long holdId, ReleaseSchedulingHoldRequest request, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingHoldsRelease, branchId, cancellationToken);
        var aggregate = await repository.GetHoldAsync(branchId, holdId, cancellationToken)
            ?? throw new NotFoundException("Scheduling hold was not found.");
        aggregate.Hold.MarkExpired(clock.UtcNow);
        aggregate.Hold.Release(request.ExpectedVersion, clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    public async Task<BookingResponse> ConfirmHoldAsync(
        long branchId,
        long holdId,
        ConfirmSchedulingHoldRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingBookingsConfirm, branchId, cancellationToken);
        var existing = await repository.GetBookingByHoldAsync(branchId, holdId, cancellationToken);
        if (existing is not null) return Map(existing, true);
        var aggregate = await repository.GetHoldAsync(branchId, holdId, cancellationToken)
            ?? throw new NotFoundException("Scheduling hold was not found.");
        var stakeholderId = await repository.GetPatientStakeholderIdAsync(aggregate.Hold.PatientId, cancellationToken)
            ?? throw new NotFoundException("The booking patient was not found.");
        var now = clock.UtcNow;
        var booking = Booking.Confirm(aggregate.Hold, now);
        var resources = aggregate.Reservations
            .Select(reservation => BookingResourceAllocation.FromReservation(booking, reservation, now))
            .ToArray();
        var operationId = Guid.NewGuid();
        var outbox = OutboxMessage.Enqueue(
            branch.TenantId,
            branchId,
            BookingConfirmedOutboxPayload.MessageType,
            JsonSerializer.Serialize(new BookingConfirmedOutboxPayload(
                booking.Id,
                branch.TenantId,
                branch.OrganizationId,
                branchId,
                booking.PatientId,
                stakeholderId)),
            now,
            operationId,
            correlationContext.CorrelationId);
        var audit = AuditEvent.Record(
            branch.TenantId,
            branchId,
            actor.ActorId,
            "Booking.Confirmed",
            nameof(Booking),
            booking.Id,
            JsonSerializer.Serialize(new
            {
                booking.HoldId,
                booking.PatientId,
                booking.ServiceId,
                ResourceCount = resources.Length,
                NotificationOperationId = operationId
            }),
            correlationContext.CorrelationId,
            now);
        var result = await repository.ConfirmHoldAtomicallyAsync(
            booking,
            resources,
            audit,
            outbox,
            request.ExpectedVersion,
            now,
            cancellationToken);
        return Map(result.Aggregate, result.IsReplay);
    }

    public async Task<BookingResponse> GetBookingAsync(
        long branchId,
        long bookingId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingBookingsView, branchId, cancellationToken);
        var booking = await repository.GetBookingAsync(branchId, bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking was not found.");
        return Map(booking, false);
    }

    public async Task<BookingResponse> CancelBookingAsync(
        long branchId,
        long bookingId,
        CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingBookingsCancel, branchId, cancellationToken);
        var current = await repository.GetBookingAsync(branchId, bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking was not found.");
        var stakeholderId = await repository.GetPatientStakeholderIdAsync(current.Booking.PatientId, cancellationToken)
            ?? throw new NotFoundException("The booking patient was not found.");
        var now = clock.UtcNow;
        var operationId = Guid.NewGuid();
        var outbox = OutboxMessage.Enqueue(
            branch.TenantId,
            branchId,
            BookingLifecycleOutboxPayload.CancelledMessageType,
            JsonSerializer.Serialize(new BookingLifecycleOutboxPayload(
                bookingId, branch.TenantId, branch.OrganizationId, branchId,
                current.Booking.PatientId, stakeholderId)),
            now,
            operationId,
            correlationContext.CorrelationId);
        var audit = AuditEvent.Record(
            branch.TenantId,
            branchId,
            actor.ActorId,
            "Booking.Cancelled",
            nameof(Booking),
            bookingId,
            JsonSerializer.Serialize(new { request.Reason, NotificationOperationId = operationId }),
            correlationContext.CorrelationId,
            now);
        var result = await repository.CancelBookingAtomicallyAsync(
            branchId, bookingId, request.ExpectedVersion, request.Reason, audit, outbox, now, cancellationToken);
        return Map(result, false);
    }

    public async Task<BookingResponse> RescheduleBookingAsync(
        long branchId,
        long bookingId,
        RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingBookingsReschedule, branchId, cancellationToken);
        var current = await repository.GetBookingAsync(branchId, bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking was not found.");
        var replacementHoldId = publicIds.Decode(PublicIdKind.SchedulingHold, request.ReplacementHoldId, branch.TenantId);
        var existingReplacement = await repository.GetBookingByHoldAsync(branchId, replacementHoldId, cancellationToken);
        if (existingReplacement is not null)
        {
            if (existingReplacement.Booking.PreviousBookingId != bookingId)
                throw new DomainRuleException("The replacement hold already belongs to another booking.");
            return Map(existingReplacement, true);
        }
        var hold = await repository.GetHoldAsync(branchId, replacementHoldId, cancellationToken)
            ?? throw new NotFoundException("Replacement scheduling hold was not found.");
        var stakeholderId = await repository.GetPatientStakeholderIdAsync(current.Booking.PatientId, cancellationToken)
            ?? throw new NotFoundException("The booking patient was not found.");
        var now = clock.UtcNow;
        var replacement = Booking.RescheduleFrom(hold.Hold, current.Booking, now);
        var resources = hold.Reservations
            .Select(reservation => BookingResourceAllocation.FromReservation(replacement, reservation, now)).ToArray();
        var operationId = Guid.NewGuid();
        var outbox = OutboxMessage.Enqueue(
            branch.TenantId,
            branchId,
            BookingLifecycleOutboxPayload.RescheduledMessageType,
            JsonSerializer.Serialize(new BookingLifecycleOutboxPayload(
                replacement.Id, branch.TenantId, branch.OrganizationId, branchId,
                replacement.PatientId, stakeholderId, current.Booking.Id)),
            now,
            operationId,
            correlationContext.CorrelationId);
        var audit = AuditEvent.Record(
            branch.TenantId,
            branchId,
            actor.ActorId,
            "Booking.Rescheduled",
            nameof(Booking),
            current.Booking.Id,
            JsonSerializer.Serialize(new
            {
                ReplacementBookingId = replacement.Id,
                ReplacementHoldId = replacement.HoldId,
                request.Reason,
                NotificationOperationId = operationId
            }),
            correlationContext.CorrelationId,
            now);
        var result = await repository.RescheduleBookingAtomicallyAsync(
            current.Booking.Id, replacement, resources, request.ExpectedBookingVersion,
            request.ExpectedHoldVersion, request.Reason, audit, outbox, now, cancellationToken);
        return Map(result.Aggregate, result.IsReplay);
    }

    public async Task<BookingWaitlistResponse> CreateWaitlistAsync(
        long branchId,
        CreateBookingWaitlistRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingWaitlistManage, branchId, cancellationToken);
        var patientId = publicIds.Decode(PublicIdKind.Patient, request.PatientId, branch.TenantId);
        var serviceId = publicIds.Decode(PublicIdKind.ClinicalService, request.ServiceId, branch.TenantId);
        if (!await repository.PatientExistsAsync(patientId, cancellationToken)) throw new NotFoundException("Patient was not found.");
        if (!await repository.ServiceExistsAsync(serviceId, cancellationToken)) throw new NotFoundException("Service was not found.");
        var now = clock.UtcNow;
        var entry = BookingWaitlistEntry.Create(
            branch.TenantId, branchId, patientId, serviceId, request.EarliestStartUtc,
            request.LatestStartUtc, request.Priority, request.Reason, now);
        var audit = AuditEvent.Record(
            branch.TenantId, branchId, actor.ActorId, "BookingWaitlist.Created",
            nameof(BookingWaitlistEntry), entry.Id,
            JsonSerializer.Serialize(new { patientId, serviceId, entry.Priority }),
            correlationContext.CorrelationId, now);
        await repository.AddWaitlistAsync(entry, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(entry);
    }

    public async Task<BookingWaitlistResponse> GetWaitlistAsync(
        long branchId,
        long waitlistId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingWaitlistView, branchId, cancellationToken);
        var entry = await repository.GetWaitlistAsync(branchId, waitlistId, false, cancellationToken)
            ?? throw new NotFoundException("Waitlist entry was not found.");
        return Map(entry);
    }

    public async Task<BookingWaitlistResponse> WithdrawWaitlistAsync(
        long branchId,
        long waitlistId,
        WithdrawBookingWaitlistRequest request,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingWaitlistManage, branchId, cancellationToken);
        var entry = await repository.GetWaitlistAsync(branchId, waitlistId, true, cancellationToken)
            ?? throw new NotFoundException("Waitlist entry was not found.");
        var now = clock.UtcNow;
        entry.Withdraw(request.ExpectedVersion, request.Reason, now);
        await repository.AddAuditEventAsync(AuditEvent.Record(
            entry.TenantId, branchId, actor.ActorId, "BookingWaitlist.Withdrawn",
            nameof(BookingWaitlistEntry), entry.Id,
            JsonSerializer.Serialize(new { request.Reason }), correlationContext.CorrelationId, now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(entry);
    }

    public async Task<BookingResponse> PromoteWaitlistAsync(
        long branchId,
        long waitlistId,
        PromoteBookingWaitlistRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingWaitlistManage, branchId, cancellationToken);
        var entry = await repository.GetWaitlistAsync(branchId, waitlistId, false, cancellationToken)
            ?? throw new NotFoundException("Waitlist entry was not found.");
        if (entry.Status == BookingWaitlistStatus.Promoted && entry.PromotedBookingId.HasValue)
        {
            var existing = await repository.GetBookingAsync(branchId, entry.PromotedBookingId.Value, cancellationToken)
                ?? throw new NotFoundException("The promoted booking was not found.");
            return Map(existing, true);
        }
        var holdId = publicIds.Decode(PublicIdKind.SchedulingHold, request.HoldId, branch.TenantId);
        var hold = await repository.GetHoldAsync(branchId, holdId, cancellationToken)
            ?? throw new NotFoundException("Promotion scheduling hold was not found.");
        var stakeholderId = await repository.GetPatientStakeholderIdAsync(entry.PatientId, cancellationToken)
            ?? throw new NotFoundException("The waitlist patient was not found.");
        var now = clock.UtcNow;
        var booking = Booking.PromoteFromWaitlist(hold.Hold, entry, now);
        var resources = hold.Reservations
            .Select(reservation => BookingResourceAllocation.FromReservation(booking, reservation, now)).ToArray();
        var operationId = Guid.NewGuid();
        var outbox = OutboxMessage.Enqueue(
            branch.TenantId,
            branchId,
            BookingLifecycleOutboxPayload.WaitlistPromotedMessageType,
            JsonSerializer.Serialize(new BookingLifecycleOutboxPayload(
                booking.Id, branch.TenantId, branch.OrganizationId, branchId,
                booking.PatientId, stakeholderId)),
            now,
            operationId,
            correlationContext.CorrelationId);
        var audit = AuditEvent.Record(
            branch.TenantId, branchId, actor.ActorId, "BookingWaitlist.Promoted",
            nameof(BookingWaitlistEntry), entry.Id,
            JsonSerializer.Serialize(new { BookingId = booking.Id, HoldId = booking.HoldId, NotificationOperationId = operationId }),
            correlationContext.CorrelationId, now);
        var result = await repository.PromoteWaitlistAtomicallyAsync(
            entry.Id, booking, resources, request.ExpectedWaitlistVersion, request.ExpectedHoldVersion,
            audit, outbox, now, cancellationToken);
        return Map(result.Booking, result.IsReplay);
    }

    private async Task<Branch> RequireBranchAsync(string permission, long branchId, CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this scheduling operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private AvailabilityResourceResponse Evaluate(BookDoc2026.Domain.Catalog.BookableResource resource, long serviceId,
        DateTimeOffset startUtc, DateTimeOffset endUtc, int requested, IReadOnlyCollection<AvailabilityRule> rules,
        IReadOnlyCollection<AvailabilityException> exceptions, int reserved)
    {
        var reason = SchedulingAvailabilityEvaluator.GetUnavailableReason(resource, serviceId, startUtc, endUtc, requested, reserved, rules, exceptions, out var capacity);
        return new(publicIds.Encode(PublicIdKind.BookableResource, resource.Id, resource.TenantId), resource.Code, resource.Name, capacity, reserved, Math.Max(0, capacity - reserved), reason is null, reason);
    }

    private AvailabilityRuleResponse Map(AvailabilityRule rule) => new(
        publicIds.Encode(PublicIdKind.AvailabilityRule, rule.Id, rule.TenantId),
        publicIds.Encode(PublicIdKind.BookableResource, rule.ResourceId, rule.TenantId),
        publicIds.EncodeOptional(PublicIdKind.ClinicalService, rule.ServiceId, rule.TenantId),
        rule.DayOfWeek.ToString(), rule.LocalStart, rule.LocalEnd, rule.EffectiveFrom, rule.EffectiveTo,
        rule.SlotIntervalMinutes, rule.Capacity, rule.IsActive, rule.Version);
    private AvailabilityExceptionResponse Map(AvailabilityException item) => new(
        publicIds.Encode(PublicIdKind.AvailabilityException, item.Id, item.TenantId),
        publicIds.Encode(PublicIdKind.BookableResource, item.ResourceId, item.TenantId),
        item.StartUtc, item.EndUtc, item.Kind.ToString(), item.CapacityOverride, item.Reason);
    private SchedulingHoldResponse Map(SchedulingHoldAggregate aggregate) => new(
        publicIds.Encode(PublicIdKind.SchedulingHold, aggregate.Hold.Id, aggregate.Hold.TenantId),
        aggregate.Hold.RequestId,
        publicIds.Encode(PublicIdKind.Patient, aggregate.Hold.PatientId, aggregate.Hold.TenantId),
        publicIds.Encode(PublicIdKind.ClinicalService, aggregate.Hold.ServiceId, aggregate.Hold.TenantId), aggregate.Hold.StartUtc,
        aggregate.Hold.EndUtc, aggregate.Hold.ExpiresUtc, aggregate.Hold.Status.ToString(), aggregate.Hold.Version,
        aggregate.Reservations.Select(item => new HoldResourceResponse(
            publicIds.Encode(PublicIdKind.BookableResource, item.ResourceId, item.TenantId),
            item.Quantity,
            item.RequirementRoleCode)).ToArray());

    private BookingResponse Map(BookingAggregate aggregate, bool isReplay) => new(
        publicIds.Encode(PublicIdKind.Booking, aggregate.Booking.Id, aggregate.Booking.TenantId),
        aggregate.Booking.BookingNumber,
        publicIds.Encode(PublicIdKind.SchedulingHold, aggregate.Booking.HoldId, aggregate.Booking.TenantId),
        publicIds.Encode(PublicIdKind.Patient, aggregate.Booking.PatientId, aggregate.Booking.TenantId),
        publicIds.Encode(PublicIdKind.ClinicalService, aggregate.Booking.ServiceId, aggregate.Booking.TenantId),
        aggregate.Booking.StartUtc,
        aggregate.Booking.EndUtc,
        aggregate.Booking.Status.ToString(),
        aggregate.Booking.ConfirmedUtc,
        aggregate.Booking.CancelledUtc,
        aggregate.Booking.CancellationReason,
        publicIds.EncodeOptional(PublicIdKind.Booking, aggregate.Booking.PreviousBookingId, aggregate.Booking.TenantId),
        publicIds.EncodeOptional(PublicIdKind.Booking, aggregate.Booking.ReplacedByBookingId, aggregate.Booking.TenantId),
        publicIds.EncodeOptional(PublicIdKind.BookingWaitlistEntry, aggregate.Booking.WaitlistEntryId, aggregate.Booking.TenantId),
        aggregate.Booking.Version,
        aggregate.Resources.Select(resource => new BookingResourceResponse(
            publicIds.Encode(PublicIdKind.BookableResource, resource.ResourceId, resource.TenantId),
            resource.Quantity,
            resource.RequirementRoleCode)).ToArray(),
        true,
        isReplay);

    private BookingWaitlistResponse Map(BookingWaitlistEntry entry) => new(
        publicIds.Encode(PublicIdKind.BookingWaitlistEntry, entry.Id, entry.TenantId),
        publicIds.Encode(PublicIdKind.Patient, entry.PatientId, entry.TenantId),
        publicIds.Encode(PublicIdKind.ClinicalService, entry.ServiceId, entry.TenantId),
        entry.EarliestStartUtc,
        entry.LatestStartUtc,
        entry.Priority,
        entry.Reason,
        entry.Status.ToString(),
        publicIds.EncodeOptional(PublicIdKind.Booking, entry.PromotedBookingId, entry.TenantId),
        entry.Version);
}
