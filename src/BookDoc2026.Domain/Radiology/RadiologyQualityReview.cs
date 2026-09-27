using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Radiology;

public sealed class RadiologyQualityReview : TenantScopedEntity
{
    private RadiologyQualityReview() { }

    public long BranchId { get; private set; }
    public long StudyId { get; private set; }
    public long AcquisitionAttemptId { get; private set; }
    public Guid RequestId { get; private set; }
    public RadiologyQualityDecision Decision { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public long ReviewedByActorId { get; private set; }

    internal static RadiologyQualityReview Record(
        RadiologyStudy study,
        RadiologyAcquisitionAttempt attempt,
        Guid requestId,
        RadiologyQualityDecision decision,
        string reasonCode,
        string? note,
        long reviewerActorId,
        DateTimeOffset now)
    {
        if (requestId == Guid.Empty || reviewerActorId <= 0 || !Enum.IsDefined(decision))
            throw new DomainRuleException("Quality-review identity, decision and reviewer are required.");
        return new RadiologyQualityReview
        {
            TenantId = study.TenantId,
            BranchId = study.BranchId,
            StudyId = study.Id,
            AcquisitionAttemptId = attempt.Id,
            RequestId = requestId,
            Decision = decision,
            ReasonCode = RadiologyAcquisitionAttempt.RequiredCode(reasonCode, "Quality-review reason code"),
            Note = RadiologyAcquisitionAttempt.OptionalText(note, 500, "Quality-review note"),
            ReviewedByActorId = reviewerActorId,
            CreatedUtc = now,
            ModifiedUtc = now
        };
    }
}
