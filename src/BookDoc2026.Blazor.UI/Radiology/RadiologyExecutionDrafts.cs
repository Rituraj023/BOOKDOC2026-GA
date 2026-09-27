using BookDoc2026.Contracts.Radiology;

namespace BookDoc2026.Blazor.UI.Radiology;

public sealed class RadiologyAcquisitionDraft
{
    public Guid RequestId { get; private set; } = Guid.NewGuid();
    public string EquipmentResourceId { get; set; } = string.Empty;
    public string ProtocolCode { get; set; } = "XR-GENERAL";
    public string ProtocolVersion { get; set; } = "V1";
    public bool HasProtocolDeviation { get; set; }
    public string DeviationCode { get; set; } = string.Empty;
    public string DeviationNote { get; set; } = string.Empty;
    public string Outcome { get; set; } = "Acquired";
    public string OutcomeReasonCode { get; set; } = string.Empty;
    public string OutcomeNote { get; set; } = string.Empty;
    public string ExternalStudyReference { get; set; } = string.Empty;
    public DateTime StartedLocal { get; set; } = DateTime.Now.AddMinutes(-1);
    public DateTime CompletedLocal { get; set; } = DateTime.Now;

    public bool CanSubmit => Required(EquipmentResourceId, ProtocolCode, ProtocolVersion)
        && CompletedLocal >= StartedLocal
        && (!HasProtocolDeviation || !string.IsNullOrWhiteSpace(DeviationCode))
        && (Outcome != "Aborted" || !string.IsNullOrWhiteSpace(OutcomeReasonCode));

    public RecordRadiologyAcquisitionRequest ToRequest(long expectedVersion) => new(
        RequestId,
        expectedVersion,
        EquipmentResourceId.Trim(),
        ProtocolCode.Trim(),
        ProtocolVersion.Trim(),
        HasProtocolDeviation,
        NullIfWhiteSpace(DeviationCode),
        NullIfWhiteSpace(DeviationNote),
        Outcome,
        NullIfWhiteSpace(OutcomeReasonCode),
        NullIfWhiteSpace(OutcomeNote),
        NullIfWhiteSpace(ExternalStudyReference),
        new DateTimeOffset(StartedLocal).ToUniversalTime(),
        new DateTimeOffset(CompletedLocal).ToUniversalTime());

    public void Reset(string? equipmentResourceId = null)
    {
        RequestId = Guid.NewGuid();
        EquipmentResourceId = equipmentResourceId ?? string.Empty;
        ProtocolCode = "XR-GENERAL";
        ProtocolVersion = "V1";
        HasProtocolDeviation = false;
        DeviationCode = DeviationNote = OutcomeReasonCode = OutcomeNote = ExternalStudyReference = string.Empty;
        Outcome = "Acquired";
        StartedLocal = DateTime.Now.AddMinutes(-1);
        CompletedLocal = DateTime.Now;
    }

    private static bool Required(params string[] values) =>
        values.All(value => !string.IsNullOrWhiteSpace(value));

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class RadiologyQualityDraft
{
    public Guid RequestId { get; private set; } = Guid.NewGuid();
    public string AcquisitionAttemptId { get; private set; } = string.Empty;
    public string Decision { get; set; } = "Accepted";
    public string ReasonCode { get; set; } = "ACCEPTED";
    public string Note { get; set; } = string.Empty;

    public bool CanSubmit => !string.IsNullOrWhiteSpace(AcquisitionAttemptId)
        && !string.IsNullOrWhiteSpace(ReasonCode);

    public void LoadAttempt(string acquisitionAttemptId)
    {
        if (string.Equals(AcquisitionAttemptId, acquisitionAttemptId, StringComparison.Ordinal)) return;
        AcquisitionAttemptId = acquisitionAttemptId;
        RequestId = Guid.NewGuid();
        Decision = "Accepted";
        ReasonCode = "ACCEPTED";
        Note = string.Empty;
    }

    public ReviewRadiologyQualityRequest ToRequest(long expectedVersion) => new(
        RequestId,
        expectedVersion,
        AcquisitionAttemptId,
        Decision,
        ReasonCode.Trim(),
        string.IsNullOrWhiteSpace(Note) ? null : Note.Trim());

    public void Reset()
    {
        RequestId = Guid.NewGuid();
        AcquisitionAttemptId = string.Empty;
        Decision = "Accepted";
        ReasonCode = "ACCEPTED";
        Note = string.Empty;
    }
}
