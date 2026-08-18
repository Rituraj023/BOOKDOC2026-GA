using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Communications;

public sealed class CommunicationPreferenceEvent : TenantScopedEntity
{
    private CommunicationPreferenceEvent() { }

    public long BranchId { get; private set; }
    public long StakeholderId { get; private set; }
    public string PurposeCode { get; private set; } = string.Empty;
    public CommunicationMessageClass MessageClass { get; private set; }
    public CommunicationChannel Channel { get; private set; }
    public CommunicationPreferenceDecision Decision { get; private set; }
    public TimeOnly? QuietHoursStart { get; private set; }
    public TimeOnly? QuietHoursEnd { get; private set; }
    public string TimeZoneId { get; private set; } = string.Empty;
    public string EvidenceSource { get; private set; } = string.Empty;
    public string? EvidenceReference { get; private set; }
    public long RecordedByActorId { get; private set; }

    public static CommunicationPreferenceEvent Record(
        long tenantId,
        long branchId,
        long stakeholderId,
        string purposeCode,
        CommunicationMessageClass messageClass,
        CommunicationChannel channel,
        CommunicationPreferenceDecision decision,
        TimeOnly? quietHoursStart,
        TimeOnly? quietHoursEnd,
        string timeZoneId,
        string evidenceSource,
        string? evidenceReference,
        long recordedByActorId,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0 || stakeholderId <= 0 || recordedByActorId <= 0)
            throw new DomainRuleException("Communication preference identity is invalid.");
        if (!Enum.IsDefined(messageClass) || !Enum.IsDefined(channel) || !Enum.IsDefined(decision))
            throw new DomainRuleException("Communication preference classification is invalid.");
        ValidateText(purposeCode, 80, "Purpose code");
        ValidateText(timeZoneId, 100, "Time zone");
        ValidateText(evidenceSource, 80, "Evidence source");
        if (evidenceReference?.Length > 200)
            throw new DomainRuleException("Evidence reference exceeds the supported length.");
        if (quietHoursStart.HasValue != quietHoursEnd.HasValue)
            throw new DomainRuleException("Quiet hours require both start and end times.");
        if (quietHoursStart.HasValue && quietHoursStart == quietHoursEnd)
            throw new DomainRuleException("Quiet hours cannot cover an undefined full-day interval.");

        var preference = new CommunicationPreferenceEvent
        {
            TenantId = tenantId,
            BranchId = branchId,
            StakeholderId = stakeholderId,
            PurposeCode = purposeCode.Trim().ToUpperInvariant(),
            MessageClass = messageClass,
            Channel = channel,
            Decision = decision,
            QuietHoursStart = quietHoursStart,
            QuietHoursEnd = quietHoursEnd,
            TimeZoneId = timeZoneId.Trim(),
            EvidenceSource = evidenceSource.Trim(),
            EvidenceReference = string.IsNullOrWhiteSpace(evidenceReference) ? null : evidenceReference.Trim(),
            RecordedByActorId = recordedByActorId
        };
        preference.StampCreated(now);
        return preference;
    }

    private static void ValidateText(string value, int maximum, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximum)
            throw new DomainRuleException($"{label} is required and must not exceed {maximum} characters.");
    }
}
