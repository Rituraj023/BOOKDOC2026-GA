using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookDoc2026.Infrastructure.Security;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    BookDocDbContext dbContext,
    IPublicIdCodec publicIds,
    IOptions<JwtSettings> options) : IAuthService
{
    private readonly JwtSettings _settings = options.Value;

    public async Task<AuthSessionResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("The email or password is invalid.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var isPlatformOperator = roles.Contains(BookDocRoleNames.PlatformOperator, StringComparer.Ordinal);
        var scope = isPlatformOperator
            ? null
            : await ResolveRequestedScopeAsync(user.Id, request, cancellationToken);

        return await IssueSessionAsync(user, roles, isPlatformOperator, scope, cancellationToken);
    }

    public async Task<AuthSessionResponse> RefreshAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        var hash = HashToken(request.RefreshToken);
        var user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.RefreshTokenHash == hash,
            cancellationToken);
        if (user is null || !user.IsActive || user.RefreshTokenExpiresUtc <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedException("The refresh token is invalid or expired.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var isPlatformOperator = roles.Contains(BookDocRoleNames.PlatformOperator, StringComparer.Ordinal);
        ApplicationUserScope? scope = null;
        if (!isPlatformOperator)
        {
            if (user.RefreshTokenScopeId is not > 0)
            {
                throw new UnauthorizedException("The refresh token has no active organization scope.");
            }

            scope = await dbContext.UserScopes.SingleOrDefaultAsync(
                candidate => candidate.Id == user.RefreshTokenScopeId && candidate.UserId == user.Id && candidate.IsActive,
                cancellationToken)
                ?? throw new UnauthorizedException("The refresh-token scope is no longer active.");
        }

        return await IssueSessionAsync(user, roles, isPlatformOperator, scope, cancellationToken);
    }

    public async Task RevokeAsync(uint userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("The authenticated user was not found.");
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresUtc = null;
        user.RefreshTokenScopeId = null;
        user.ModifiedUtc = DateTimeOffset.UtcNow;
        var result = await userManager.UpdateAsync(user);
        EnsureIdentitySuccess(result);
    }

    private async Task<ApplicationUserScope> ResolveRequestedScopeAsync(
        uint userId,
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            return await dbContext.UserScopes.SingleOrDefaultAsync(
                scope => scope.UserId == userId && scope.IsActive && scope.IsDefault,
                cancellationToken)
                ?? throw new UnauthorizedException("Select an active clinic organization scope for this account.");
        }

        if (string.IsNullOrWhiteSpace(request.OrganizationId))
        {
            throw new UnauthorizedException("An organization identifier is required with the tenant identifier.");
        }

        var tenantId = publicIds.Decode(PublicIdKind.Tenant, request.TenantId);
        var organizationId = publicIds.Decode(PublicIdKind.Organization, request.OrganizationId, tenantId);
        var branchId = publicIds.DecodeOptional(PublicIdKind.Branch, request.BranchId, tenantId);
        return await dbContext.UserScopes.SingleOrDefaultAsync(
            scope => scope.UserId == userId
                && scope.TenantId == tenantId
                && scope.OrganizationId == organizationId
                && scope.BranchId == branchId
                && scope.IsActive,
            cancellationToken)
            ?? throw new UnauthorizedException("The user is not assigned to the selected clinic scope.");
    }

    private async Task<AuthSessionResponse> IssueSessionAsync(
        ApplicationUser user,
        IEnumerable<string> roles,
        bool isPlatformOperator,
        ApplicationUserScope? scope,
        CancellationToken cancellationToken)
    {
        ValidateSettings();
        var roleList = roles.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var permissions = await LoadPermissionsAsync(roleList);
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roleList.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(BookDocClaimTypes.Permission, permission)));
        if (isPlatformOperator)
        {
            claims.Add(new Claim(BookDocClaimTypes.IsPlatformOperator, bool.TrueString));
        }
        else if (scope is not null)
        {
            claims.Add(new Claim(BookDocClaimTypes.ScopeId, scope.Id.ToString()));
            claims.Add(new Claim(BookDocClaimTypes.TenantId, scope.TenantId.ToString()));
            claims.Add(new Claim(BookDocClaimTypes.OrganizationId, scope.OrganizationId.ToString()));
            if (scope.BranchId.HasValue)
            {
                claims.Add(new Claim(BookDocClaimTypes.BranchId, scope.BranchId.Value.ToString()));
            }
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _settings.Issuer,
            _settings.Audience,
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            credentials);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        user.RefreshTokenHash = HashToken(refreshToken);
        user.RefreshTokenExpiresUtc = now.AddDays(_settings.RefreshTokenDays);
        user.RefreshTokenScopeId = scope?.Id;
        user.ModifiedUtc = now;
        EnsureIdentitySuccess(await userManager.UpdateAsync(user));

        return new AuthSessionResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            refreshToken,
            expires,
            user.DisplayName,
            user.Email ?? string.Empty,
            isPlatformOperator,
            Map(scope),
            roleList,
            permissions);
    }

    private async Task<IReadOnlyCollection<string>> LoadPermissionsAsync(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null || !role.IsActive) continue;
            var claims = await roleManager.GetClaimsAsync(role);
            permissions.UnionWith(claims
                .Where(claim => claim.Type == BookDocClaimTypes.Permission)
                .Select(claim => claim.Value));
        }

        return permissions.Order(StringComparer.Ordinal).ToArray();
    }

    private AuthScopeResponse? Map(ApplicationUserScope? scope) => scope is null
        ? null
        : new AuthScopeResponse(
            publicIds.Encode(PublicIdKind.Tenant, scope.TenantId),
            publicIds.Encode(PublicIdKind.Organization, scope.OrganizationId, scope.TenantId),
            publicIds.EncodeOptional(PublicIdKind.Branch, scope.BranchId, scope.TenantId),
            scope.IsDefault);

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Issuer)
            || string.IsNullOrWhiteSpace(_settings.Audience)
            || Encoding.UTF8.GetByteCount(_settings.Key) < 32
            || _settings.AccessTokenMinutes is < 1 or > 120
            || _settings.RefreshTokenDays is < 1 or > 90)
        {
            throw new InvalidOperationException(
                "JWT configuration is invalid. Supply a secret key of at least 32 bytes through approved secret management.");
        }
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static void EnsureIdentitySuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new DomainRuleException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
