using Microsoft.AspNetCore.Identity;

namespace BookDoc2026.Domain.Identity;

public sealed class ApplicationUser : IdentityUser<uint>
{
    public long? StakeholderId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool WhatsAppNumberConfirmed { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? RefreshTokenHash { get; set; }

    public DateTimeOffset? RefreshTokenExpiresUtc { get; set; }

    public long? RefreshTokenScopeId { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ApplicationUserScope> ScopeAssignments { get; set; } = [];
}

public sealed class ApplicationRole : IdentityRole<uint>
{
    public bool IsPlatformRole { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class ApplicationUserClaim : IdentityUserClaim<uint>;

public sealed class ApplicationUserRole : IdentityUserRole<uint>;

public sealed class ApplicationUserLogin : IdentityUserLogin<uint>;

public sealed class ApplicationRoleClaim : IdentityRoleClaim<uint>;

public sealed class ApplicationUserToken : IdentityUserToken<uint>
{
    public bool IsBlocked { get; set; }
}
