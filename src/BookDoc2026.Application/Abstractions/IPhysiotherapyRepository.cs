using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Abstractions;

public sealed record PhysiotherapyCarePlanAggregate(
    PhysiotherapyCarePlan Plan,
    IReadOnlyCollection<PhysiotherapyCarePlanRevision> Revisions,
    IReadOnlyCollection<PhysiotherapyTreatmentSession> Sessions,
    IReadOnlyCollection<PhysiotherapyOutcomeObservation> Outcomes)
{
    public PhysiotherapyCarePlanRevision LatestRevision =>
        Revisions.Single(item => item.Id == Plan.LatestRevisionId);
}

public sealed record PhysiotherapyCarePlanWorkItem(
    PhysiotherapyCarePlan Plan,
    string PatientNumber,
    string PatientDisplayName,
    DateOnly? ReviewOn,
    int SessionCount,
    DateTimeOffset? LastSessionUtc);

public interface IPhysiotherapyRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<bool> CarePlanExistsForEncounterAsync(long branchId, long encounterId,
        CancellationToken cancellationToken);
    Task<bool> SessionExistsForEncounterAsync(long branchId, long encounterId,
        CancellationToken cancellationToken);
    Task<PhysiotherapyCarePlanAggregate?> GetCarePlanAsync(long branchId, long carePlanId, bool tracked,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PhysiotherapyCarePlanWorkItem>> ListCarePlansAsync(long branchId, int take,
        CancellationToken cancellationToken);
    Task AddCarePlanAsync(PhysiotherapyCarePlan plan, PhysiotherapyCarePlanRevision revision,
        AuditEvent audit, CancellationToken cancellationToken);
    Task AddRevisionAsync(PhysiotherapyCarePlanRevision revision, AuditEvent audit,
        CancellationToken cancellationToken);
    Task AddSessionAsync(PhysiotherapyTreatmentSession session, AuditEvent audit,
        CancellationToken cancellationToken);
    Task AddOutcomeAsync(PhysiotherapyOutcomeObservation outcome, AuditEvent audit,
        CancellationToken cancellationToken);
    Task AddAuditAsync(AuditEvent audit, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
