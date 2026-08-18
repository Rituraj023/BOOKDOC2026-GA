using System.Security.Claims;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Shared.Kernel.Security;

namespace BookDoc2026.Api.Security;

public sealed class HttpCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public long ActorId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public long? TenantId => long.TryParse(User.FindFirstValue(BookDocClaimTypes.TenantId), out var id) ? id : null;

    public bool IsPlatformOperator => bool.TryParse(
        User.FindFirstValue(BookDocClaimTypes.IsPlatformOperator),
        out var value) && value;

    public IReadOnlySet<long> BranchIds => User.FindAll(BookDocClaimTypes.BranchId)
        .Select(claim => long.TryParse(claim.Value, out var id) ? id : 0)
        .Where(id => id > 0)
        .ToHashSet();

    public bool HasPermission(string permission) =>
        User.FindAll(BookDocClaimTypes.Permission)
            .Any(claim => string.Equals(claim.Value, permission, StringComparison.Ordinal));
}
