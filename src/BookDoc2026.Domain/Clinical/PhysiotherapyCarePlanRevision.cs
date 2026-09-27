using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed class PhysiotherapyCarePlanRevision : TenantScopedEntity
{
    private PhysiotherapyCarePlanRevision() { }

    public long BranchId { get; private set; }
    public long CarePlanId { get; private set; }
    public int RevisionNumber { get; private set; }
    public long? ParentRevisionId { get; private set; }
    public long AuthorActorId { get; private set; }
    public string GoalSummary { get; private set; } = string.Empty;
    public string FrequencyAndDuration { get; private set; } = string.Empty;
    public string PlannedInterventions { get; private set; } = string.Empty;
    public string? Precautions { get; private set; }
    public DateOnly? ReviewOn { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string? ChangeReason { get; private set; }

    public PhysiotherapyCarePlanContent Content => PhysiotherapyCarePlanContent.Create(
        GoalSummary, FrequencyAndDuration, PlannedInterventions, Precautions, ReviewOn);

    internal static PhysiotherapyCarePlanRevision Create(PhysiotherapyCarePlan plan, int revisionNumber,
        long? parentRevisionId, PhysiotherapyCarePlanContent content, long actorId, string? changeReason,
        DateTimeOffset now)
    {
        if (revisionNumber <= 0 || actorId <= 0)
            throw new DomainRuleException("A valid care-plan revision and author are required.");
        var revision = new PhysiotherapyCarePlanRevision
        {
            TenantId = plan.TenantId, BranchId = plan.BranchId, CarePlanId = plan.Id,
            RevisionNumber = revisionNumber, ParentRevisionId = parentRevisionId, AuthorActorId = actorId,
            GoalSummary = content.GoalSummary, FrequencyAndDuration = content.FrequencyAndDuration,
            PlannedInterventions = content.PlannedInterventions, Precautions = content.Precautions,
            ReviewOn = content.ReviewOn, ContentHash = content.Hash(), ChangeReason = changeReason
        };
        revision.StampCreated(now);
        return revision;
    }
}
