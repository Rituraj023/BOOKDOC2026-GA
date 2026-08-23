namespace BookDoc2026.Contracts.Clinical;

public sealed record EncounterContentRequest(
    string SpecialtyCode,
    string TemplateKey,
    string TemplateVersion,
    string ChiefComplaint,
    string? History,
    string? Examination,
    string? Assessment,
    string? Plan,
    string? Instructions,
    string? BodySite,
    string? LateralityCode);

public sealed record StartEncounterRequest(string BookingId, EncounterContentRequest Content);

public sealed record ReviseEncounterDraftRequest(long ExpectedVersion, EncounterContentRequest Content);

public sealed record SignEncounterRequest(long ExpectedVersion);

public sealed record AmendEncounterRequest(long ExpectedVersion, string Reason, EncounterContentRequest Content);

public sealed record EncounterRevisionResponse(
    string Id,
    int RevisionNumber,
    string Kind,
    string? ParentRevisionId,
    string AuthorId,
    EncounterContentRequest Content,
    string ContentHash,
    string? AmendmentReason,
    DateTimeOffset? SignedUtc,
    DateTimeOffset CreatedUtc);

public sealed record EncounterResponse(
    string Id,
    string BookingId,
    string PatientId,
    string ServiceId,
    string EncounterNumber,
    string Status,
    int LatestRevisionNumber,
    DateTimeOffset? SignedUtc,
    string? SignedByActorId,
    long Version,
    IReadOnlyCollection<EncounterRevisionResponse> Revisions);
