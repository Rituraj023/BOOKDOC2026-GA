using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class EncounterRepository(BookDocDbContext dbContext) : IEncounterRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public Task<Booking?> GetBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken) =>
        dbContext.Bookings.AsNoTracking().SingleOrDefaultAsync(
            item => item.BranchId == branchId && item.Id == bookingId, cancellationToken);

    public Task<bool> EncounterExistsForBookingAsync(long branchId, long bookingId,
        CancellationToken cancellationToken) => dbContext.ClinicalEncounters.AnyAsync(
        item => item.BranchId == branchId && item.BookingId == bookingId, cancellationToken);

    public async Task<EncounterAggregate?> GetEncounterAsync(long branchId, long encounterId, bool tracked,
        CancellationToken cancellationToken)
    {
        var encounters = dbContext.ClinicalEncounters.Where(
            item => item.BranchId == branchId && item.Id == encounterId);
        var encounter = await (tracked ? encounters : encounters.AsNoTracking())
            .SingleOrDefaultAsync(cancellationToken);
        if (encounter is null) return null;
        var revisions = dbContext.EncounterRevisions.Where(item => item.EncounterId == encounterId);
        return new EncounterAggregate(encounter, await (tracked ? revisions : revisions.AsNoTracking())
            .OrderBy(item => item.RevisionNumber).ToArrayAsync(cancellationToken));
    }

    public async Task AddEncounterAsync(ClinicalEncounter encounter, EncounterRevision revision,
        AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await dbContext.ClinicalEncounters.AddAsync(encounter, cancellationToken);
        await dbContext.EncounterRevisions.AddAsync(revision, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public async Task AddRevisionAsync(EncounterRevision revision, AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.EncounterRevisions.AddAsync(revision, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Clinical encounter data changed while it was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Clinical encounter data conflicts with an existing booking or revision.");
        }
    }
}
