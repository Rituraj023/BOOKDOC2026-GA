using System.Data;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class SchedulingRepository(BookDocDbContext dbContext) : ISchedulingRepository
{
    private static readonly SemaphoreSlim InMemoryHoldGate = new(1, 1);

    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(x => x.Id == branchId, cancellationToken);

    public Task<BookableResource?> GetResourceAsync(long branchId, long resourceId, CancellationToken cancellationToken) =>
        dbContext.BookableResources.SingleOrDefaultAsync(x => x.BranchId == branchId && x.Id == resourceId, cancellationToken);

    public Task<bool> ServiceExistsAsync(long serviceId, CancellationToken cancellationToken) =>
        dbContext.ClinicalServices.AnyAsync(x => x.Id == serviceId && x.Status == CatalogItemStatus.Active, cancellationToken);

    public Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken) =>
        dbContext.Patients.AnyAsync(x => x.Id == patientId, cancellationToken);

    public Task<long?> GetPatientStakeholderIdAsync(long patientId, CancellationToken cancellationToken) =>
        dbContext.Patients
            .Where(patient => patient.Id == patientId)
            .Select(patient => (long?)patient.StakeholderId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> ResourceSupportsServiceAsync(long resourceId, long serviceId, CancellationToken cancellationToken) =>
        dbContext.ResourceCapabilities.AnyAsync(x => x.ResourceId == resourceId && x.ServiceId == serviceId && x.IsActive, cancellationToken);

    public Task AddRuleAsync(AvailabilityRule rule, CancellationToken cancellationToken) =>
        dbContext.AvailabilityRules.AddAsync(rule, cancellationToken).AsTask();

    public Task AddExceptionAsync(AvailabilityException exception, CancellationToken cancellationToken) =>
        dbContext.AvailabilityExceptions.AddAsync(exception, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<BookableResource>> ListServiceResourcesAsync(long branchId, long serviceId, CancellationToken cancellationToken)
    {
        var ids = dbContext.ResourceCapabilities.Where(x => x.ServiceId == serviceId && x.IsActive).Select(x => x.ResourceId);
        return await dbContext.BookableResources.Where(x => x.BranchId == branchId && ids.Contains(x.Id))
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AvailabilityRule>> ListRulesAsync(long branchId, IReadOnlyCollection<long> resourceIds, CancellationToken cancellationToken) =>
        await dbContext.AvailabilityRules.Where(x => x.BranchId == branchId && resourceIds.Contains(x.ResourceId) && x.IsActive).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AvailabilityException>> ListExceptionsAsync(long branchId, IReadOnlyCollection<long> resourceIds,
        DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken) =>
        await dbContext.AvailabilityExceptions.Where(x => x.BranchId == branchId && resourceIds.Contains(x.ResourceId)
            && x.StartUtc < endUtc && x.EndUtc > startUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<long, int>> GetReservedQuantitiesAsync(IReadOnlyCollection<long> resourceIds,
        DateTimeOffset startUtc, DateTimeOffset endUtc, DateTimeOffset now, CancellationToken cancellationToken) =>
        await (from reservation in dbContext.ResourceReservations
               join hold in dbContext.SchedulingHolds on new { reservation.TenantId, Id = reservation.HoldId } equals new { hold.TenantId, hold.Id }
               where resourceIds.Contains(reservation.ResourceId) && reservation.StartUtc < endUtc && reservation.EndUtc > startUtc
                   && ((hold.Status == SchedulingHoldStatus.Active && hold.ExpiresUtc > now)
                       || (hold.Status == SchedulingHoldStatus.Confirmed
                           && dbContext.Bookings.Any(booking => booking.HoldId == hold.Id && booking.Status == BookingStatus.Confirmed)))
               group reservation by reservation.ResourceId into groupRows
               select new { ResourceId = groupRows.Key, Quantity = groupRows.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Quantity, cancellationToken);

    public async Task<HoldCreationResult> CreateHoldAtomicallyAsync(SchedulingHold hold,
        IReadOnlyCollection<ResourceReservation> reservations, AuditEvent auditEvent, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            await InMemoryHoldGate.WaitAsync(cancellationToken);
            try { return await CreateHoldCoreAsync(hold, reservations, auditEvent, now, cancellationToken); }
            finally { InMemoryHoldGate.Release(); }
        }

        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await CreateRelationalHoldCoreAsync(hold, reservations, auditEvent, now, cancellationToken);
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await CreateRelationalHoldCoreAsync(hold, reservations, auditEvent, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task<HoldCreationResult> CreateRelationalHoldCoreAsync(SchedulingHold hold,
        IReadOnlyCollection<ResourceReservation> reservations, AuditEvent auditEvent, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_getapplock @Resource={"BOOKDOC:HOLD:" + hold.TenantId + ":" + hold.RequestId}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000", cancellationToken);
        foreach (var resourceId in reservations.Select(x => x.ResourceId).Order())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_getapplock @Resource={"BOOKDOC:RESOURCE:" + hold.TenantId + ":" + resourceId}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000", cancellationToken);
        }

        return await CreateHoldCoreAsync(hold, reservations, auditEvent, now, cancellationToken);
    }

    private async Task<HoldCreationResult> CreateHoldCoreAsync(SchedulingHold hold,
        IReadOnlyCollection<ResourceReservation> reservations, AuditEvent auditEvent, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.SchedulingHolds.SingleOrDefaultAsync(x => x.RequestId == hold.RequestId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.PayloadHash, hold.PayloadHash, StringComparison.Ordinal))
                throw new DomainRuleException("The scheduling request identifier was reused with different content.");
            var existingReservations = await dbContext.ResourceReservations.Where(x => x.HoldId == existing.Id).ToListAsync(cancellationToken);
            return new(new(existing, existingReservations), true);
        }

        var resourceIds = reservations.Select(x => x.ResourceId).ToArray();
        var resources = await dbContext.BookableResources.Where(x => x.BranchId == hold.BranchId && resourceIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (resources.Count != resourceIds.Length) throw new NotFoundException("One or more resources were not found in this branch.");
        var supported = await dbContext.ResourceCapabilities.Where(x => resourceIds.Contains(x.ResourceId) && x.ServiceId == hold.ServiceId && x.IsActive)
            .Select(x => x.ResourceId).Distinct().ToListAsync(cancellationToken);
        if (supported.Count != resourceIds.Length) throw new DomainRuleException("Every held resource must support the selected service.");

        var rules = await ListRulesAsync(hold.BranchId, resourceIds, cancellationToken);
        var exceptions = await ListExceptionsAsync(hold.BranchId, resourceIds, hold.StartUtc, hold.EndUtc, cancellationToken);
        var reserved = await GetReservedQuantitiesAsync(resourceIds, hold.StartUtc, hold.EndUtc, now, cancellationToken);
        foreach (var reservation in reservations)
        {
            var reason = SchedulingAvailabilityEvaluator.GetUnavailableReason(resources[reservation.ResourceId], hold.ServiceId,
                hold.StartUtc, hold.EndUtc, reservation.Quantity, reserved.GetValueOrDefault(reservation.ResourceId),
                rules, exceptions, out _);
            if (reason is not null) throw new ConcurrencyConflictException(reason);
        }

        await dbContext.SchedulingHolds.AddAsync(hold, cancellationToken);
        await dbContext.ResourceReservations.AddRangeAsync(reservations, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return new(new(hold, reservations), false);
    }

    public async Task<SchedulingHoldAggregate?> GetHoldAsync(long branchId, long holdId, CancellationToken cancellationToken)
    {
        var hold = await dbContext.SchedulingHolds.SingleOrDefaultAsync(x => x.BranchId == branchId && x.Id == holdId, cancellationToken);
        if (hold is null) return null;
        var reservations = await dbContext.ResourceReservations.Where(x => x.HoldId == hold.Id).ToListAsync(cancellationToken);
        return new(hold, reservations);
    }

    public async Task<BookingConfirmationResult> ConfirmHoldAtomicallyAsync(
        Booking booking,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        long expectedHoldVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            await InMemoryHoldGate.WaitAsync(cancellationToken);
            try
            {
                return await ConfirmHoldCoreAsync(
                    booking, resources, auditEvent, outboxMessage, expectedHoldVersion, now, cancellationToken);
            }
            finally
            {
                InMemoryHoldGate.Release();
            }
        }

        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await ConfirmRelationalCoreAsync(
                booking, resources, auditEvent, outboxMessage, expectedHoldVersion, now, cancellationToken);
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var result = await ConfirmRelationalCoreAsync(
                booking, resources, auditEvent, outboxMessage, expectedHoldVersion, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task<BookingConfirmationResult> ConfirmRelationalCoreAsync(
        Booking booking,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        long expectedHoldVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_getapplock @Resource={"BOOKDOC:CONFIRM:" + booking.TenantId + ":" + booking.HoldId}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000",
            cancellationToken);
        foreach (var resourceId in resources.Select(resource => resource.ResourceId).Order())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_getapplock @Resource={"BOOKDOC:RESOURCE:" + booking.TenantId + ":" + resourceId}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000",
                cancellationToken);
        }

        return await ConfirmHoldCoreAsync(
            booking, resources, auditEvent, outboxMessage, expectedHoldVersion, now, cancellationToken);
    }

    private async Task<BookingConfirmationResult> ConfirmHoldCoreAsync(
        Booking booking,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        long expectedHoldVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Bookings
            .SingleOrDefaultAsync(candidate => candidate.HoldId == booking.HoldId, cancellationToken);
        if (existing is not null)
        {
            var existingResources = await dbContext.BookingResourceAllocations
                .Where(resource => resource.BookingId == existing.Id)
                .ToListAsync(cancellationToken);
            return new(new(existing, existingResources), true);
        }

        var hold = await dbContext.SchedulingHolds
            .SingleOrDefaultAsync(candidate => candidate.BranchId == booking.BranchId
                && candidate.Id == booking.HoldId, cancellationToken)
            ?? throw new NotFoundException("Scheduling hold was not found.");
        if (dbContext.Database.IsRelational())
            await dbContext.Entry(hold).ReloadAsync(cancellationToken);
        var reservations = await dbContext.ResourceReservations
            .Where(reservation => reservation.HoldId == hold.Id)
            .ToListAsync(cancellationToken);
        if (booking.TenantId != hold.TenantId
            || booking.PatientId != hold.PatientId
            || booking.ServiceId != hold.ServiceId
            || booking.StartUtc != hold.StartUtc
            || booking.EndUtc != hold.EndUtc)
            throw new DomainRuleException("Booking details do not match the held schedule.");

        var allocationByReservation = resources.ToDictionary(resource => resource.HoldReservationId);
        if (allocationByReservation.Count != reservations.Count
            || reservations.Any(reservation =>
                !allocationByReservation.TryGetValue(reservation.Id, out var allocation)
                || allocation.ResourceId != reservation.ResourceId
                || allocation.Quantity != reservation.Quantity
                || !string.Equals(
                    allocation.RequirementRoleCode,
                    reservation.RequirementRoleCode,
                    StringComparison.Ordinal)))
            throw new DomainRuleException("Booking resources do not match the held reservations.");

        var resourceIds = reservations.Select(reservation => reservation.ResourceId).ToArray();
        var bookableResources = await dbContext.BookableResources
            .Where(resource => resource.BranchId == hold.BranchId && resourceIds.Contains(resource.Id))
            .ToDictionaryAsync(resource => resource.Id, cancellationToken);
        var requirements = await dbContext.ServiceResourceRequirements
            .Where(requirement => requirement.ServiceId == hold.ServiceId)
            .ToListAsync(cancellationToken);
        SchedulingRequirementEvaluator.EnsureComplete(reservations, bookableResources, requirements);

        hold.Confirm(expectedHoldVersion, now);
        await dbContext.Bookings.AddAsync(booking, cancellationToken);
        await dbContext.BookingResourceAllocations.AddRangeAsync(resources, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        await dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return new(new(booking, resources), false);
    }

    public async Task<BookingAggregate?> GetBookingAsync(
        long branchId,
        long bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.BranchId == branchId && candidate.Id == bookingId,
                cancellationToken);
        if (booking is null) return null;
        var resources = await dbContext.BookingResourceAllocations
            .AsNoTracking()
            .Where(resource => resource.BookingId == booking.Id)
            .ToListAsync(cancellationToken);
        return new(booking, resources);
    }

    public async Task<BookingAggregate?> GetBookingByHoldAsync(
        long branchId,
        long holdId,
        CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.BranchId == branchId && candidate.HoldId == holdId,
                cancellationToken);
        if (booking is null) return null;
        var resources = await dbContext.BookingResourceAllocations
            .AsNoTracking()
            .Where(resource => resource.BookingId == booking.Id)
            .ToListAsync(cancellationToken);
        return new(booking, resources);
    }

    public async Task<BookingAggregate> CancelBookingAtomicallyAsync(
        long branchId,
        long bookingId,
        long expectedVersion,
        string reason,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await ExecuteLifecycleAsync(
            $"BOOKDOC:BOOKING:{bookingId}",
            async () =>
            {
                var booking = await dbContext.Bookings.SingleOrDefaultAsync(
                    candidate => candidate.BranchId == branchId && candidate.Id == bookingId,
                    cancellationToken) ?? throw new NotFoundException("Booking was not found.");
                if (dbContext.Database.IsRelational()) await dbContext.Entry(booking).ReloadAsync(cancellationToken);
                var resources = await dbContext.BookingResourceAllocations
                    .Where(resource => resource.BookingId == booking.Id).ToListAsync(cancellationToken);
                booking.Cancel(expectedVersion, reason, now);
                await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
                await dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                await SaveChangesAsync(cancellationToken);
                return new BookingAggregate(booking, resources);
            },
            cancellationToken);

    public async Task<BookingConfirmationResult> RescheduleBookingAtomicallyAsync(
        long originalBookingId,
        Booking replacement,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        long expectedBookingVersion,
        long expectedHoldVersion,
        string reason,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await ExecuteLifecycleAsync(
            $"BOOKDOC:RESCHEDULE:{replacement.TenantId}:{originalBookingId}:{replacement.HoldId}",
            async () =>
            {
                var existing = await dbContext.Bookings.SingleOrDefaultAsync(
                    candidate => candidate.HoldId == replacement.HoldId, cancellationToken);
                if (existing is not null)
                {
                    if (existing.PreviousBookingId != originalBookingId)
                        throw new DomainRuleException("The replacement hold already belongs to another booking.");
                    var existingResources = await dbContext.BookingResourceAllocations
                        .Where(resource => resource.BookingId == existing.Id).ToListAsync(cancellationToken);
                    return new BookingConfirmationResult(new(existing, existingResources), true);
                }

                var original = await dbContext.Bookings.SingleOrDefaultAsync(
                    candidate => candidate.Id == originalBookingId && candidate.BranchId == replacement.BranchId,
                    cancellationToken) ?? throw new NotFoundException("Booking was not found.");
                if (dbContext.Database.IsRelational()) await dbContext.Entry(original).ReloadAsync(cancellationToken);
                var hold = await LoadAndValidateReplacementHoldAsync(replacement, resources, cancellationToken);
                original.ReplaceWith(replacement.Id, expectedBookingVersion, reason, now);
                hold.Confirm(expectedHoldVersion, now);
                await dbContext.Bookings.AddAsync(replacement, cancellationToken);
                await dbContext.BookingResourceAllocations.AddRangeAsync(resources, cancellationToken);
                await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
                await dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                await SaveChangesAsync(cancellationToken);
                return new BookingConfirmationResult(new(replacement, resources), false);
            },
            cancellationToken);

    public Task AddWaitlistAsync(BookingWaitlistEntry entry, AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        dbContext.BookingWaitlistEntries.Add(entry);
        dbContext.AuditEvents.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken).AsTask();

    public async Task<BookingWaitlistEntry?> GetWaitlistAsync(
        long branchId,
        long waitlistId,
        bool tracked,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BookingWaitlistEntries.Where(entry => entry.BranchId == branchId && entry.Id == waitlistId);
        return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<WaitlistPromotionResult> PromoteWaitlistAtomicallyAsync(
        long waitlistId,
        Booking booking,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        long expectedWaitlistVersion,
        long expectedHoldVersion,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await ExecuteLifecycleAsync(
            $"BOOKDOC:WAITLIST:{booking.TenantId}:{waitlistId}",
            async () =>
            {
                var waitlist = await dbContext.BookingWaitlistEntries.SingleOrDefaultAsync(
                    entry => entry.Id == waitlistId && entry.BranchId == booking.BranchId,
                    cancellationToken) ?? throw new NotFoundException("Waitlist entry was not found.");
                if (dbContext.Database.IsRelational()) await dbContext.Entry(waitlist).ReloadAsync(cancellationToken);
                if (waitlist.Status == BookingWaitlistStatus.Promoted && waitlist.PromotedBookingId.HasValue)
                {
                    var existing = await dbContext.Bookings.SingleAsync(
                        candidate => candidate.Id == waitlist.PromotedBookingId.Value, cancellationToken);
                    var existingResources = await dbContext.BookingResourceAllocations
                        .Where(resource => resource.BookingId == existing.Id).ToListAsync(cancellationToken);
                    return new WaitlistPromotionResult(new(existing, existingResources), waitlist, true);
                }

                var hold = await LoadAndValidateReplacementHoldAsync(booking, resources, cancellationToken);
                if (waitlist.PatientId != booking.PatientId || waitlist.ServiceId != booking.ServiceId
                    || booking.StartUtc < waitlist.EarliestStartUtc || booking.StartUtc > waitlist.LatestStartUtc)
                    throw new DomainRuleException("The held schedule does not match the waitlist entry.");
                hold.Confirm(expectedHoldVersion, now);
                waitlist.Promote(booking.Id, expectedWaitlistVersion, now);
                await dbContext.Bookings.AddAsync(booking, cancellationToken);
                await dbContext.BookingResourceAllocations.AddRangeAsync(resources, cancellationToken);
                await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
                await dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                await SaveChangesAsync(cancellationToken);
                return new WaitlistPromotionResult(new(booking, resources), waitlist, false);
            },
            cancellationToken);

    private async Task<SchedulingHold> LoadAndValidateReplacementHoldAsync(
        Booking booking,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        CancellationToken cancellationToken)
    {
        var hold = await dbContext.SchedulingHolds.SingleOrDefaultAsync(
            candidate => candidate.Id == booking.HoldId && candidate.BranchId == booking.BranchId,
            cancellationToken) ?? throw new NotFoundException("Replacement scheduling hold was not found.");
        if (dbContext.Database.IsRelational()) await dbContext.Entry(hold).ReloadAsync(cancellationToken);
        var reservations = await dbContext.ResourceReservations
            .Where(reservation => reservation.HoldId == hold.Id).ToListAsync(cancellationToken);
        if (booking.TenantId != hold.TenantId || booking.PatientId != hold.PatientId
            || booking.ServiceId != hold.ServiceId || booking.StartUtc != hold.StartUtc || booking.EndUtc != hold.EndUtc)
            throw new DomainRuleException("Booking details do not match the replacement hold.");
        var allocationByReservation = resources.ToDictionary(resource => resource.HoldReservationId);
        if (allocationByReservation.Count != reservations.Count || reservations.Any(reservation =>
                !allocationByReservation.TryGetValue(reservation.Id, out var allocation)
                || allocation.ResourceId != reservation.ResourceId || allocation.Quantity != reservation.Quantity
                || !string.Equals(allocation.RequirementRoleCode, reservation.RequirementRoleCode, StringComparison.Ordinal)))
            throw new DomainRuleException("Booking resources do not match the replacement hold.");
        var resourceIds = reservations.Select(reservation => reservation.ResourceId).ToArray();
        var bookableResources = await dbContext.BookableResources
            .Where(resource => resource.BranchId == hold.BranchId && resourceIds.Contains(resource.Id))
            .ToDictionaryAsync(resource => resource.Id, cancellationToken);
        var requirements = await dbContext.ServiceResourceRequirements
            .Where(requirement => requirement.ServiceId == hold.ServiceId).ToListAsync(cancellationToken);
        SchedulingRequirementEvaluator.EnsureComplete(reservations, bookableResources, requirements);
        return hold;
    }

    private async Task<T> ExecuteLifecycleAsync<T>(
        string lockName,
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            await InMemoryHoldGate.WaitAsync(cancellationToken);
            try { return await action(); }
            finally { InMemoryHoldGate.Release(); }
        }

        if (dbContext.Database.CurrentTransaction is not null)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_getapplock @Resource={lockName}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000",
                cancellationToken);
            return await action();
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_getapplock @Resource={lockName}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000",
                cancellationToken);
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

        public Task AddSlotsAsync(IEnumerable<BookingSlot> slots, CancellationToken cancellationToken) =>
        dbContext.BookingSlots.AddRangeAsync(slots, cancellationToken);

    public Task<BookingSlot?> GetSlotAsync(long branchId, long slotId, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? dbContext.BookingSlots : dbContext.BookingSlots.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.BranchId == branchId && x.Id == slotId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<BookingSlot>> ListSlotsAsync(
        long branchId,
        long? practitionerId,
        long? serviceId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BookingSlots.AsNoTracking().Where(x => x.BranchId == branchId);
        if (practitionerId.HasValue) query = query.Where(x => x.PractitionerId == practitionerId.Value);
        if (serviceId.HasValue) query = query.Where(x => x.ServiceId == serviceId.Value);
        if (fromDate.HasValue) query = query.Where(x => x.SlotDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.SlotDate <= toDate.Value);

        return await query.OrderBy(x => x.SlotDate).ThenBy(x => x.StartUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BookingSlot>> ListAvailableSlotsAsync(
        long branchId,
        long? practitionerId,
        long? serviceId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BookingSlots.AsNoTracking().Where(x => x.BranchId == branchId &&
            (x.Status == BookingSlotStatus.Available || x.Status == BookingSlotStatus.PartiallyBooked) &&
            x.BookedCount < x.MaxCapacity);

        if (practitionerId.HasValue) query = query.Where(x => x.PractitionerId == practitionerId.Value);
        if (serviceId.HasValue) query = query.Where(x => x.ServiceId == serviceId.Value);
        if (fromDate.HasValue) query = query.Where(x => x.SlotDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.SlotDate <= toDate.Value);

        return await query.OrderBy(x => x.SlotDate).ThenBy(x => x.StartUtc).ToListAsync(cancellationToken);
    }

    public Task<bool> SlotOverlapsAsync(long branchId, long practitionerId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken) =>
        dbContext.BookingSlots.AnyAsync(x =>
            x.BranchId == branchId &&
            x.PractitionerId == practitionerId &&
            x.Status != BookingSlotStatus.Cancelled &&
            x.StartUtc < endUtc &&
            x.EndUtc > startUtc,
            cancellationToken);

    public Task AddBookingRequestAsync(BookingRequest request, CancellationToken cancellationToken) =>
        dbContext.BookingRequests.AddAsync(request, cancellationToken).AsTask();

    public Task<BookingRequest?> GetBookingRequestAsync(long branchId, long requestId, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? dbContext.BookingRequests : dbContext.BookingRequests.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.BranchId == branchId && x.Id == requestId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<BookingRequest>> ListBookingRequestsAsync(
        long branchId,
        BookingRequestStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BookingRequests.AsNoTracking().Where(x => x.BranchId == branchId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (fromDate.HasValue) query = query.Where(x => x.PreferredDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.PreferredDate <= toDate.Value);

        return await query.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("The scheduling record changed while it was being saved."); }
        catch (DbUpdateException) { throw new DomainRuleException("Scheduling data conflicts with an existing request or relationship."); }
    }
}
