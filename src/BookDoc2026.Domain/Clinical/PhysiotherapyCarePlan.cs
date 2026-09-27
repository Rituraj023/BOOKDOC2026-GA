using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed class PhysiotherapyCarePlan : TenantScopedEntity
{
    private PhysiotherapyCarePlan() { }

    public long BranchId { get; private set; }
    public long PatientId { get; private set; }
    public long ServiceId { get; private set; }
    public long InitialEncounterId { get; private set; }
    public string CarePlanNumber { get; private set; } = string.Empty;
    public PhysiotherapyCarePlanStatus Status { get; private set; }
    public int LatestRevisionNumber { get; private set; }
    public long LatestRevisionId { get; private set; }
    public long CreatedByActorId { get; private set; }
    public DateTimeOffset? ActivatedUtc { get; private set; }
    public long? ActivatedByActorId { get; private set; }
    public DateTimeOffset? ClosedUtc { get; private set; }
    public long? ClosedByActorId { get; private set; }
    public string? ClosureReason { get; private set; }
    public long Version { get; private set; } = 1;

    public static (PhysiotherapyCarePlan Plan, PhysiotherapyCarePlanRevision Revision) Start(
        ClinicalEncounter encounter, EncounterRevision latestEncounterRevision,
        PhysiotherapyCarePlanContent content, long actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(latestEncounterRevision);
        ArgumentNullException.ThrowIfNull(content);
        if (encounter.Status != EncounterStatus.Signed || latestEncounterRevision.EncounterId != encounter.Id
            || latestEncounterRevision.Id != encounter.LatestRevisionId
            || !string.Equals(latestEncounterRevision.SpecialtyCode, "PHYSIOTHERAPY", StringComparison.Ordinal))
            throw new DomainRuleException("A signed Physiotherapy encounter is required to start a care plan.");
        if (actorId <= 0) throw new DomainRuleException("A care-plan author is required.");
        var plan = new PhysiotherapyCarePlan
        {
            TenantId = encounter.TenantId,
            BranchId = encounter.BranchId,
            PatientId = encounter.PatientId,
            ServiceId = encounter.ServiceId,
            InitialEncounterId = encounter.Id,
            CarePlanNumber = $"PT-{encounter.EncounterNumber}",
            Status = PhysiotherapyCarePlanStatus.Draft,
            CreatedByActorId = actorId
        };
        plan.StampCreated(now);
        var revision = PhysiotherapyCarePlanRevision.Create(plan, 1, null, content, actorId, null, now);
        plan.LatestRevisionNumber = 1;
        plan.LatestRevisionId = revision.Id;
        return (plan, revision);
    }

    public PhysiotherapyCarePlanRevision Revise(PhysiotherapyCarePlanRevision latest,
        PhysiotherapyCarePlanContent content, string? reason, long expectedVersion, long actorId, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureLatest(latest);
        if (Status is not (PhysiotherapyCarePlanStatus.Draft or PhysiotherapyCarePlanStatus.Active))
            throw new DomainRuleException("Only a draft or active care plan can be revised.");
        var normalizedReason = Status == PhysiotherapyCarePlanStatus.Active
            ? PhysiotherapyText.Required(reason ?? string.Empty, 5, 500, "Active-plan change reason")
            : PhysiotherapyText.Optional(reason, 500);
        var revision = PhysiotherapyCarePlanRevision.Create(this, LatestRevisionNumber + 1, latest.Id,
            content, actorId, normalizedReason, now);
        LatestRevisionNumber = revision.RevisionNumber;
        LatestRevisionId = revision.Id;
        Version++;
        StampModified(now);
        return revision;
    }

    public void Activate(long expectedVersion, long actorId, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status != PhysiotherapyCarePlanStatus.Draft)
            throw new DomainRuleException("Only a draft care plan can be activated.");
        Status = PhysiotherapyCarePlanStatus.Active;
        ActivatedUtc = now;
        ActivatedByActorId = actorId;
        Version++;
        StampModified(now);
    }

    public void Close(bool discontinued, string reason, long expectedVersion, long actorId, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status != PhysiotherapyCarePlanStatus.Active)
            throw new DomainRuleException("Only an active care plan can be closed.");
        Status = discontinued ? PhysiotherapyCarePlanStatus.Discontinued : PhysiotherapyCarePlanStatus.Completed;
        ClosureReason = PhysiotherapyText.Required(reason, 5, 1000, "Closure reason");
        ClosedUtc = now;
        ClosedByActorId = actorId;
        Version++;
        StampModified(now);
    }

    private void EnsureLatest(PhysiotherapyCarePlanRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        if (revision.CarePlanId != Id || revision.Id != LatestRevisionId
            || revision.RevisionNumber != LatestRevisionNumber || revision.TenantId != TenantId)
            throw new DomainRuleException("The supplied care-plan revision is not current.");
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The Physiotherapy care plan changed after it was loaded.");
    }
}
