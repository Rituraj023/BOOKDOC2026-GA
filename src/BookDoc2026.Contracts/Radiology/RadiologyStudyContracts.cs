namespace BookDoc2026.Contracts.Radiology;

public sealed record RegisterRadiologyStudyRequest(Guid RequestId);

public sealed record StartRadiologyStudyRequest(Guid RequestId, long ExpectedVersion);

public sealed record RecordRadiologyAcquisitionRequest(
    Guid RequestId,
    long ExpectedVersion,
    string EquipmentResourceId,
    string ProtocolCode,
    string ProtocolVersion,
    bool HasProtocolDeviation,
    string? DeviationCode,
    string? DeviationNote,
    string Outcome,
    string? OutcomeReasonCode,
    string? OutcomeNote,
    string? ExternalStudyReference,
    DateTimeOffset StartedUtc,
    DateTimeOffset CompletedUtc);

public sealed record ReviewRadiologyQualityRequest(
    Guid RequestId,
    long ExpectedVersion,
    string AcquisitionAttemptId,
    string Decision,
    string ReasonCode,
    string? Note);

public sealed record RadiologyEquipmentOptionResponse(
    string Id,
    string Code,
    string Name);

public sealed record RadiologyAcquisitionAttemptResponse(
    string Id,
    int Sequence,
    string EquipmentResourceId,
    string ProtocolCode,
    string ProtocolVersion,
    bool HasProtocolDeviation,
    string? DeviationCode,
    string? DeviationNote,
    string Outcome,
    string? OutcomeReasonCode,
    string? OutcomeNote,
    bool HasExternalStudyReference,
    string PerformedByActorId,
    DateTimeOffset StartedUtc,
    DateTimeOffset CompletedUtc);

public sealed record RadiologyQualityReviewResponse(
    string Id,
    string AcquisitionAttemptId,
    string Decision,
    string ReasonCode,
    string? Note,
    string ReviewedByActorId,
    DateTimeOffset ReviewedUtc);

public sealed record RadiologyStudyEventResponse(
    string Id,
    string Action,
    string ActorId,
    long StudyVersion,
    DateTimeOffset OccurredUtc);

public sealed record RadiologyStudyResponse(
    string Id,
    string OrderId,
    string PatientId,
    string ServiceId,
    string Modality,
    string Status,
    string RegisteredByActorId,
    DateTimeOffset RegisteredUtc,
    string? LastStartedByActorId,
    DateTimeOffset? LastStartedUtc,
    int AcquisitionAttemptCount,
    long Version,
    IReadOnlyCollection<RadiologyAcquisitionAttemptResponse> AcquisitionAttempts,
    IReadOnlyCollection<RadiologyQualityReviewResponse> QualityReviews,
    IReadOnlyCollection<RadiologyStudyEventResponse> History,
    bool IsReplay);
