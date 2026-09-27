using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Radiology;

public sealed class RadiologyStudyEvent : TenantScopedEntity
{
    private RadiologyStudyEvent() { }

    public long BranchId { get; private set; }
    public long StudyId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public long ActorId { get; private set; }
    public long StudyVersion { get; private set; }

    public static RadiologyStudyEvent Record(
        RadiologyStudy study,
        Guid requestId,
        string requestFingerprint,
        string action,
        long actorId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(study);
        var normalizedAction = RadiologyAcquisitionAttempt.RequiredCode(action, "Study event action");
        var normalizedFingerprint = requestFingerprint?.Trim().ToUpperInvariant() ?? string.Empty;
        if (requestId == Guid.Empty || actorId <= 0 || normalizedFingerprint.Length != 64
            || normalizedFingerprint.Any(character => !Uri.IsHexDigit(character)))
            throw new DomainRuleException("Study event request identity, fingerprint and actor are required.");
        var item = new RadiologyStudyEvent
        {
            TenantId = study.TenantId,
            BranchId = study.BranchId,
            StudyId = study.Id,
            RequestId = requestId,
            RequestFingerprint = normalizedFingerprint,
            Action = normalizedAction,
            ActorId = actorId,
            StudyVersion = study.Version
        };
        item.StampCreated(now);
        return item;
    }
}
