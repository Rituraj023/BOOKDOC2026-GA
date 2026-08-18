using System.Security.Claims;
using System.Text.Encodings.Web;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BookDoc2026.Api.Security;

public sealed class DevelopmentHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHostEnvironment environment,
    IPublicIdCodec publicIds)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "BookDocDevelopmentHeaders";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                "Development header authentication is disabled outside Development and Testing."));
        }

        if (!Request.Headers.TryGetValue("X-User-Id", out var userIdValue)
            || !long.TryParse(userIdValue, out var userId)
            || userId <= 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, $"development-{userId}")
        };

        long? tenantId = null;
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var tenantValue))
        {
            try
            {
                tenantId = publicIds.Decode(PublicIdKind.Tenant, tenantValue.ToString());
                claims.Add(new Claim(BookDocClaimTypes.TenantId, tenantId.Value.ToString()));
            }
            catch
            {
                return Task.FromResult(AuthenticateResult.Fail("The development tenant identifier is invalid."));
            }
        }
        if (!AddBranchClaims(tenantId, claims))
            return Task.FromResult(AuthenticateResult.Fail("A development branch identifier is invalid."));
        AddStringListClaims("X-Permissions", BookDocClaimTypes.Permission, claims);

        if (Request.Headers.TryGetValue("X-Platform-Operator", out var platformValue)
            && bool.TryParse(platformValue, out var isPlatformOperator)
            && isPlatformOperator)
        {
            claims.Add(new Claim(BookDocClaimTypes.IsPlatformOperator, bool.TrueString));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool AddBranchClaims(long? tenantId, ICollection<Claim> claims)
    {
        if (!Request.Headers.TryGetValue("X-Branch-Ids", out var values))
        {
            return true;
        }

        if (tenantId is not > 0) return false;

        foreach (var value in values.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                claims.Add(new Claim(BookDocClaimTypes.BranchId,
                publicIds.Decode(PublicIdKind.Branch, value, tenantId).ToString()));
            }
            catch { return false; }
        }
        return true;
    }

    private void AddStringListClaims(string header, string claimType, ICollection<Claim> claims)
    {
        if (!Request.Headers.TryGetValue(header, out var values))
        {
            return;
        }

        foreach (var value in values.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(claimType, value));
        }
    }
}
