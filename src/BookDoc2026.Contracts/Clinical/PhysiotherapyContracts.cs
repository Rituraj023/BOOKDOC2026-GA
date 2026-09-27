namespace BookDoc2026.Contracts.Clinical;

public sealed record PhysiotherapyCarePlanContentRequest(
    string GoalSummary,
    string FrequencyAndDuration,
    string PlannedInterventions,
    string? Precautions,
    DateOnly? ReviewOn);

public sealed record CreatePhysiotherapyCarePlanRequest(
    string InitialEncounterId,
    PhysiotherapyCarePlanContentRequest Content);

public sealed record RevisePhysiotherapyCarePlanRequest(
    long ExpectedVersion,
    string? Reason,
    PhysiotherapyCarePlanContentRequest Content);

public sealed record ActivatePhysiotherapyCarePlanRequest(long ExpectedVersion);

public sealed record ChangePhysiotherapyCarePlanStatusRequest(long ExpectedVersion, string Reason);

public sealed record RecordPhysiotherapySessionRequest(
    string EncounterId,
    string SubjectiveResponse,
    string Interventions,
    string Tolerance,
    string NextPlan,
    bool HadAdverseEvent,
    string? AdverseEventDetails);

public sealed record RecordPhysiotherapyOutcomeRequest(
    string? TreatmentSessionId,
    string ContextCode,
    string MeasureCode,
    string ToolVersion,
    decimal Value,
    string Unit,
    string? BodySite,
    string? LateralityCode,
    DateTimeOffset ObservedUtc);

public sealed record PhysiotherapyCarePlanRevisionResponse(
    string Id,
    int RevisionNumber,
    string? ParentRevisionId,
    string AuthorId,
    PhysiotherapyCarePlanContentRequest Content,
    string ContentHash,
    string? ChangeReason,
    DateTimeOffset CreatedUtc);

public sealed record PhysiotherapySessionResponse(
    string Id,
    string EncounterId,
    string BookingId,
    int SequenceNumber,
    string AuthorId,
    string SubjectiveResponse,
    string Interventions,
    string Tolerance,
    string NextPlan,
    bool HadAdverseEvent,
    string? AdverseEventDetails,
    string ContentHash,
    DateTimeOffset PerformedUtc);

public sealed record PhysiotherapyOutcomeResponse(
    string Id,
    string? TreatmentSessionId,
    string ContextCode,
    string MeasureCode,
    string ToolVersion,
    decimal Value,
    string Unit,
    string? BodySite,
    string? LateralityCode,
    DateTimeOffset ObservedUtc,
    string AuthorId);

public sealed record PhysiotherapyCarePlanResponse(
    string Id,
    string PatientId,
    string ServiceId,
    string InitialEncounterId,
    string CarePlanNumber,
    string Status,
    int LatestRevisionNumber,
    DateTimeOffset? ActivatedUtc,
    string? ActivatedByActorId,
    DateTimeOffset? ClosedUtc,
    string? ClosedByActorId,
    string? ClosureReason,
    long Version,
    IReadOnlyCollection<PhysiotherapyCarePlanRevisionResponse> Revisions,
    IReadOnlyCollection<PhysiotherapySessionResponse> Sessions,
    IReadOnlyCollection<PhysiotherapyOutcomeResponse> Outcomes);

public sealed record PhysiotherapyCarePlanListItemResponse(
    string Id,
    string CarePlanNumber,
    string PatientId,
    string PatientNumber,
    string PatientDisplayName,
    string ServiceId,
    string Status,
    int LatestRevisionNumber,
    DateOnly? ReviewOn,
    int SessionCount,
    DateTimeOffset? LastSessionUtc,
    long Version);
