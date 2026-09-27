using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class PhysiotherapyRepository(BookDocDbContext db) : IPhysiotherapyRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        db.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public Task<bool> CarePlanExistsForEncounterAsync(long branchId, long encounterId,
        CancellationToken cancellationToken) => db.PhysiotherapyCarePlans.AnyAsync(
        item => item.BranchId == branchId && item.InitialEncounterId == encounterId, cancellationToken);

    public Task<bool> SessionExistsForEncounterAsync(long branchId, long encounterId,
        CancellationToken cancellationToken) => db.PhysiotherapyTreatmentSessions.AnyAsync(
        item => item.BranchId == branchId && item.EncounterId == encounterId, cancellationToken);

    public async Task<PhysiotherapyCarePlanAggregate?> GetCarePlanAsync(long branchId, long carePlanId,
        bool tracked, CancellationToken cancellationToken)
    {
        var plans = db.PhysiotherapyCarePlans.Where(item => item.BranchId == branchId && item.Id == carePlanId);
        var plan = await (tracked ? plans : plans.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        if (plan is null) return null;
        var revisions = db.PhysiotherapyCarePlanRevisions.Where(item => item.CarePlanId == carePlanId);
        var sessions = db.PhysiotherapyTreatmentSessions.Where(item => item.CarePlanId == carePlanId);
        var outcomes = db.PhysiotherapyOutcomeObservations.Where(item => item.CarePlanId == carePlanId);
        return new(plan,
            await (tracked ? revisions : revisions.AsNoTracking()).OrderBy(item => item.RevisionNumber)
                .ToArrayAsync(cancellationToken),
            await (tracked ? sessions : sessions.AsNoTracking()).OrderBy(item => item.SequenceNumber)
                .ToArrayAsync(cancellationToken),
            await (tracked ? outcomes : outcomes.AsNoTracking()).OrderBy(item => item.ObservedUtc)
                .ToArrayAsync(cancellationToken));
    }

    public async Task<IReadOnlyCollection<PhysiotherapyCarePlanWorkItem>> ListCarePlansAsync(long branchId,
        int take, CancellationToken cancellationToken)
    {
        var rows = await (
            from plan in db.PhysiotherapyCarePlans.AsNoTracking()
            join patient in db.Patients.AsNoTracking() on plan.PatientId equals patient.Id
            join stakeholder in db.Stakeholders.AsNoTracking() on patient.StakeholderId equals stakeholder.Id
            where plan.BranchId == branchId
            orderby plan.Status, plan.ModifiedUtc descending, plan.CreatedUtc descending
            select new
            {
                Plan = plan,
                patient.PatientNumber,
                PatientDisplayName = stakeholder.DisplayName,
                ReviewOn = db.PhysiotherapyCarePlanRevisions
                    .Where(revision => revision.Id == plan.LatestRevisionId)
                    .Select(revision => revision.ReviewOn)
                    .SingleOrDefault(),
                SessionCount = db.PhysiotherapyTreatmentSessions.Count(session => session.CarePlanId == plan.Id),
                LastSessionUtc = db.PhysiotherapyTreatmentSessions
                    .Where(session => session.CarePlanId == plan.Id)
                    .Max(session => (DateTimeOffset?)session.PerformedUtc)
            }).Take(take).ToArrayAsync(cancellationToken);

        return rows.Select(row => new PhysiotherapyCarePlanWorkItem(row.Plan, row.PatientNumber,
            row.PatientDisplayName, row.ReviewOn, row.SessionCount, row.LastSessionUtc)).ToArray();
    }

    public async Task AddCarePlanAsync(PhysiotherapyCarePlan plan, PhysiotherapyCarePlanRevision revision,
        AuditEvent audit, CancellationToken cancellationToken)
    {
        await db.PhysiotherapyCarePlans.AddAsync(plan, cancellationToken);
        await db.PhysiotherapyCarePlanRevisions.AddAsync(revision, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public async Task AddRevisionAsync(PhysiotherapyCarePlanRevision revision, AuditEvent audit,
        CancellationToken cancellationToken)
    {
        await db.PhysiotherapyCarePlanRevisions.AddAsync(revision, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public async Task AddSessionAsync(PhysiotherapyTreatmentSession session, AuditEvent audit,
        CancellationToken cancellationToken)
    {
        await db.PhysiotherapyTreatmentSessions.AddAsync(session, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public async Task AddOutcomeAsync(PhysiotherapyOutcomeObservation outcome, AuditEvent audit,
        CancellationToken cancellationToken)
    {
        await db.PhysiotherapyOutcomeObservations.AddAsync(outcome, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public Task AddAuditAsync(AuditEvent audit, CancellationToken cancellationToken) =>
        db.AuditEvents.AddAsync(audit, cancellationToken).AsTask();

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Physiotherapy data changed while it was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Physiotherapy data conflicts with an existing care plan or session.");
        }
    }
}
