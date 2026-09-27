namespace BookDoc2026.Contracts.Workforce;

public sealed record CreatePractitionerRequest(
    string StakeholderId,
    string IdentitySubjectId,
    string PractitionerCode,
    string PractitionerTypeCode);

public sealed record AddPractitionerCredentialRequest(
    string CredentialTypeCode,
    string RegistrationNumber,
    string IssuingAuthority,
    DateOnly ValidFrom,
    DateOnly? ValidTo);

public sealed record DecidePractitionerCredentialRequest(long ExpectedVersion, string? Reason = null);

public sealed record AddPractitionerAssignmentRequest(
    string ServiceId,
    string? BookableResourceId,
    string RoleCode,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record ChangePractitionerStatusRequest(long ExpectedVersion);

public sealed record PractitionerResponse(
    string Id,
    string StakeholderId,
    string IdentitySubjectId,
    string PractitionerCode,
    string PractitionerTypeCode,
    string Status,
    long Version,
    IReadOnlyCollection<PractitionerCredentialResponse> Credentials,
    IReadOnlyCollection<PractitionerAssignmentResponse> Assignments);

public sealed record PractitionerCredentialResponse(
    string Id,
    string CredentialTypeCode,
    string RegistrationNumber,
    string IssuingAuthority,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string VerificationStatus,
    string? VerifiedBySubjectId,
    DateTimeOffset? VerifiedUtc,
    string? DecisionReason,
    long Version);

public sealed record PractitionerAssignmentResponse(
    string Id,
    string BranchId,
    string ServiceId,
    string? BookableResourceId,
    string RoleCode,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    long Version);

public sealed record PractitionerSummaryResponse(
    string Id,
    string PractitionerCode,
    string PractitionerTypeCode,
    string DisplayName,
    string Status,
    IReadOnlyCollection<string> RoleCodes,
    IReadOnlyCollection<string> AssignedServiceIds);

