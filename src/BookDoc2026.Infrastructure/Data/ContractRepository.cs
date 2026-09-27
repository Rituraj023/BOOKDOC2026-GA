using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Contracts;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class ContractRepository(BookDocDbContext dbContext) : IContractRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken) =>
        dbContext.Patients.AnyAsync(item => item.Id == patientId, cancellationToken);

    public Task<bool> ServiceExistsAsync(long serviceId, CancellationToken cancellationToken) =>
        dbContext.ClinicalServices.AnyAsync(item => item.Id == serviceId, cancellationToken);

    public Task<bool> ResourceCategoryExistsAsync(long resourceCategoryId, CancellationToken cancellationToken) =>
        dbContext.ResourceCategories.AnyAsync(item => item.Id == resourceCategoryId, cancellationToken);

    public Task<bool> ContractNumberExistsAsync(long branchId, string contractNumber, CancellationToken cancellationToken) =>
        dbContext.Contracts.AnyAsync(item => item.BranchId == branchId && item.ContractNumber == contractNumber,
            cancellationToken);

    public Task<Booking?> GetBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken) =>
        dbContext.Bookings.AsNoTracking().SingleOrDefaultAsync(item => item.BranchId == branchId && item.Id == bookingId,
            cancellationToken);

    public Task<bool> BookingUsesResourceCategoryAsync(long bookingId, long resourceCategoryId,
        CancellationToken cancellationToken) =>
        (from allocation in dbContext.BookingResourceAllocations
         join resource in dbContext.BookableResources on allocation.ResourceId equals resource.Id
         where allocation.BookingId == bookingId && resource.CategoryId == resourceCategoryId
         select allocation.Id).AnyAsync(cancellationToken);

    public async Task AddContractAsync(ContractAgreement agreement,
        IReadOnlyCollection<ContractEntitlement> entitlements, AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.Contracts.AddAsync(agreement, cancellationToken);
        await dbContext.ContractEntitlements.AddRangeAsync(entitlements, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public async Task<ContractAggregate?> GetContractAsync(long branchId, long contractId, bool tracked,
        CancellationToken cancellationToken)
    {
        var contracts = dbContext.Contracts.Where(item => item.BranchId == branchId && item.Id == contractId);
        var agreement = await (tracked ? contracts : contracts.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        if (agreement is null) return null;
        var entitlements = dbContext.ContractEntitlements.Where(item => item.ContractId == contractId);
        return new(agreement, await (tracked ? entitlements : entitlements.AsNoTracking())
            .OrderBy(item => item.ServiceId).ThenBy(item => item.ResourceCategoryId).ToListAsync(cancellationToken));
    }

    public async Task<EntitlementReservationAggregate?> GetReservationAsync(long branchId, long reservationId,
        bool tracked, CancellationToken cancellationToken)
    {
        var reservations = dbContext.EntitlementReservations.Where(item => item.BranchId == branchId && item.Id == reservationId);
        var reservation = await (tracked ? reservations : reservations.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        if (reservation is null) return null;
        var entitlements = dbContext.ContractEntitlements.Where(item => item.Id == reservation.EntitlementId);
        var entitlement = await (tracked ? entitlements : entitlements.AsNoTracking()).SingleAsync(cancellationToken);
        return new(reservation, entitlement);
    }

    public async Task<EntitlementReservationAggregate?> GetReservationByRequestAsync(long branchId, Guid requestId,
        CancellationToken cancellationToken)
    {
        var reservation = await dbContext.EntitlementReservations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.BranchId == branchId && item.RequestId == requestId, cancellationToken);
        if (reservation is null) return null;
        var entitlement = await dbContext.ContractEntitlements.AsNoTracking()
            .SingleAsync(item => item.Id == reservation.EntitlementId, cancellationToken);
        return new(reservation, entitlement);
    }

    public async Task AddReservationAsync(EntitlementReservation reservation, AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.EntitlementReservations.AddAsync(reservation, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public async Task AddAuditAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Contract entitlement data changed while it was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Contract data conflicts with an existing number, request or relationship.");
        }
    }

    public async Task<IReadOnlyCollection<ContractAggregate>> GetPatientActiveContractsAsync(
        long branchId, long patientId, CancellationToken cancellationToken)
    {
        var agreements = await dbContext.Contracts.AsNoTracking()
            .Where(item => item.BranchId == branchId && item.PatientId == patientId && item.Status == ContractStatus.Active)
            .OrderByDescending(item => item.ValidTo)
            .ToListAsync(cancellationToken);

        var results = new List<ContractAggregate>(agreements.Count);
        foreach (var agreement in agreements)
        {
            var entitlements = await dbContext.ContractEntitlements.AsNoTracking()
                .Where(e => e.ContractId == agreement.Id)
                .OrderBy(e => e.ServiceId)
                .ToListAsync(cancellationToken);
            results.Add(new ContractAggregate(agreement, entitlements));
        }
        return results;
    }

    public async Task<EntitlementReservationAggregate?> GetActiveReservationForBookingAsync(
        long branchId, long bookingId, bool tracked, CancellationToken cancellationToken)
    {
        var reservations = dbContext.EntitlementReservations
            .Where(item => item.BranchId == branchId && item.BookingId == bookingId && item.Status == EntitlementReservationStatus.Reserved);
        var reservation = await (tracked ? reservations : reservations.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        if (reservation is null) return null;
        var entitlements = dbContext.ContractEntitlements.Where(item => item.Id == reservation.EntitlementId);
        var entitlement = await (tracked ? entitlements : entitlements.AsNoTracking()).SingleAsync(cancellationToken);
        return new(reservation, entitlement);
    }
}