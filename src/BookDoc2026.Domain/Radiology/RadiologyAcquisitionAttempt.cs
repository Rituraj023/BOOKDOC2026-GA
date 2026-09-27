using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Radiology;

public sealed class RadiologyAcquisitionAttempt : TenantScopedEntity
{
    private RadiologyAcquisitionAttempt() { }

    public long BranchId { get; private set; }
    public long StudyId { get; private set; }
    public long OrderId { get; private set; }
    public int Sequence { get; private set; }
    public Guid RequestId { get; private set; }
    public long EquipmentResourceId { get; private set; }
    public string ProtocolCode { get; private set; } = string.Empty;
    public string ProtocolVersion { get; private set; } = string.Empty;
    public bool HasProtocolDeviation { get; private set; }
    public string? DeviationCode { get; private set; }
    public string? DeviationNote { get; private set; }
    public RadiologyAcquisitionOutcome Outcome { get; private set; }
    public string? OutcomeReasonCode { get; private set; }
    public string? OutcomeNote { get; private set; }
    public string? ExternalStudyReference { get; private set; }
    public long PerformedByActorId { get; private set; }
    public DateTimeOffset StartedUtc { get; private set; }
    public DateTimeOffset CompletedUtc { get; private set; }

    internal static RadiologyAcquisitionAttempt Record(
        RadiologyStudy study,
        int sequence,
        Guid requestId,
        long equipmentResourceId,
        string protocolCode,
        string protocolVersion,
        bool hasProtocolDeviation,
        string? deviationCode,
        string? deviationNote,
        RadiologyAcquisitionOutcome outcome,
        string? outcomeReasonCode,
        string? outcomeNote,
        string? externalStudyReference,
        long actorId,
        DateTimeOffset startedUtc,
        DateTimeOffset completedUtc,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(study);
        if (sequence <= 0 || requestId == Guid.Empty || equipmentResourceId <= 0 || actorId <= 0
            || !Enum.IsDefined(outcome))
            throw new DomainRuleException("Acquisition identity, equipment, outcome and operator are required.");
        if (completedUtc < startedUtc || completedUtc - startedUtc > TimeSpan.FromHours(24)
            || startedUtc < study.RegisteredUtc.AddMinutes(-5) || completedUtc > now.AddMinutes(5))
            throw new DomainRuleException("Acquisition timing is invalid.");

        var normalizedDeviationCode = OptionalCode(deviationCode, "Protocol deviation code");
        var normalizedDeviationNote = OptionalText(deviationNote, 500, "Protocol deviation note");
        if (hasProtocolDeviation != (normalizedDeviationCode is not null))
            throw new DomainRuleException("A protocol deviation requires a coded reason, and a code is not allowed without a deviation.");
        if (!hasProtocolDeviation && normalizedDeviationNote is not null)
            throw new DomainRuleException("A protocol deviation note requires a deviation code.");

        var normalizedOutcomeCode = OptionalCode(outcomeReasonCode, "Acquisition outcome reason code");
        var normalizedOutcomeNote = OptionalText(outcomeNote, 500, "Acquisition outcome note");
        if (outcome == RadiologyAcquisitionOutcome.Aborted && normalizedOutcomeCode is null)
            throw new DomainRuleException("An aborted acquisition requires a coded reason.");
        if (outcome == RadiologyAcquisitionOutcome.Acquired
            && (normalizedOutcomeCode is not null || normalizedOutcomeNote is not null))
            throw new DomainRuleException("A successful acquisition cannot carry an abort reason.");

        var attempt = new RadiologyAcquisitionAttempt
        {
            TenantId = study.TenantId,
            BranchId = study.BranchId,
            StudyId = study.Id,
            OrderId = study.OrderId,
            Sequence = sequence,
            RequestId = requestId,
            EquipmentResourceId = equipmentResourceId,
            ProtocolCode = RequiredCode(protocolCode, "Protocol code"),
            ProtocolVersion = RequiredCode(protocolVersion, "Protocol version"),
            HasProtocolDeviation = hasProtocolDeviation,
            DeviationCode = normalizedDeviationCode,
            DeviationNote = normalizedDeviationNote,
            Outcome = outcome,
            OutcomeReasonCode = normalizedOutcomeCode,
            OutcomeNote = normalizedOutcomeNote,
            ExternalStudyReference = OptionalText(externalStudyReference, 200, "External study reference"),
            PerformedByActorId = actorId,
            StartedUtc = startedUtc,
            CompletedUtc = completedUtc
        };
        attempt.StampCreated(now);
        return attempt;
    }

    internal static string RequiredCode(string? value, string label)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is < 2 or > 40 || normalized.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
            throw new DomainRuleException($"{label} must contain 2 to 40 letters, numbers, dots, hyphens or underscores.");
        return normalized;
    }

    internal static string? OptionalCode(string? value, string label) =>
        string.IsNullOrWhiteSpace(value) ? null : RequiredCode(value, label);

    internal static string? OptionalText(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new DomainRuleException($"{label} cannot exceed {maxLength} characters.");
        return normalized;
    }
}
