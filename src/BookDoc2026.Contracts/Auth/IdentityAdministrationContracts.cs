namespace BookDoc2026.Contracts.Auth;

public sealed record CreateIdentityUserRequest(
    string DisplayName,
    string Email,
    string? PhoneNumber,
    string TemporaryPassword);

public sealed record IdentityUserResponse(
    string SubjectId,
    string DisplayName,
    string Email,
    string? PhoneNumber,
    bool IsActive);
