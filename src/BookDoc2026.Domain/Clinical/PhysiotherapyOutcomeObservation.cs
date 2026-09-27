using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed class PhysiotherapyOutcomeObservation : TenantScopedEntity
{
    private PhysiotherapyOutcomeObservation() { }

    public long BranchId { get; private set; }
    public long CarePlanId { get; private set; }
    public long? TreatmentSessionId { get; private set; }
    public string ContextCode { get; private set; } = string.Empty;
    public string MeasureCode { get; private set; } = string.Empty;
    public string ToolVersion { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string? BodySite { get; private set; }
    public string? LateralityCode { get; private set; }
    public DateTimeOffset ObservedUtc { get; private set; }
    public long AuthorActorId { get; private set; }

    public static PhysiotherapyOutcomeObservation Record(PhysiotherapyCarePlan plan,
        PhysiotherapyTreatmentSession? session, string contextCode, string measureCode, string toolVersion,
        decimal value, string unit, string? bodySite, string? lateralityCode, DateTimeOffset observedUtc,
        long actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Status is not (PhysiotherapyCarePlanStatus.Draft or PhysiotherapyCarePlanStatus.Active))
            throw new DomainRuleException("Outcomes can be recorded only for an open care plan.");
        if (session is not null && (session.CarePlanId != plan.Id || session.TenantId != plan.TenantId))
            throw new DomainRuleException("Outcome session does not belong to the care plan.");
        if (observedUtc > now.AddMinutes(5) || actorId <= 0)
            throw new DomainRuleException("Outcome observation time or author is invalid.");
        if (value is > 999999999999999m or < -999999999999999m)
            throw new DomainRuleException("Outcome value is outside the supported numeric range.");
        var observation = new PhysiotherapyOutcomeObservation
        {
            TenantId = plan.TenantId, BranchId = plan.BranchId, CarePlanId = plan.Id,
            TreatmentSessionId = session?.Id,
            ContextCode = PhysiotherapyText.Code(contextCode, 2, 40, "Outcome context"),
            MeasureCode = PhysiotherapyText.Code(measureCode, 2, 80, "Measure code"),
            ToolVersion = PhysiotherapyText.Required(toolVersion, 1, 40, "Tool version"),
            Value = decimal.Round(value, 4, MidpointRounding.AwayFromZero),
            Unit = PhysiotherapyText.Required(unit, 1, 30, "Outcome unit"),
            BodySite = PhysiotherapyText.Optional(bodySite, 200),
            LateralityCode = string.IsNullOrWhiteSpace(lateralityCode) ? null
                : PhysiotherapyText.Code(lateralityCode, 2, 30, "Laterality code"),
            ObservedUtc = observedUtc, AuthorActorId = actorId
        };
        observation.StampCreated(now);
        return observation;
    }
}
