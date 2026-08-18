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
                   && (hold.Status == SchedulingHoldStatus.Confirmed || (hold.Status == SchedulingHoldStatus.Active && hold.ExpiresUtc > now))
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

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("The scheduling record changed while it was being saved."); }
        catch (DbUpdateException) { throw new DomainRuleException("Scheduling data conflicts with an existing request or relationship."); }
    }
}
