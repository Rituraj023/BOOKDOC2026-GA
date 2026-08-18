namespace BookDoc2026.Contracts.Auth;

public sealed record LoginRequest(
    string Email,
    string Password,
    string? TenantId = null,
    string? OrganizationId = null,
    string? BranchId = null);

public sealed record RefreshSessionRequest(string RefreshToken);

public sealed record AuthScopeResponse(
    string TenantId,
    string OrganizationId,
    string? BranchId,
    bool IsDefault);

public sealed record AuthSessionResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresUtc,
    string DisplayName,
    string Email,
    bool IsPlatformOperator,
    AuthScopeResponse? Scope,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record CurrentSessionResponse(
    string DisplayName,
    string Email,
    bool IsPlatformOperator,
    AuthScopeResponse? Scope,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
