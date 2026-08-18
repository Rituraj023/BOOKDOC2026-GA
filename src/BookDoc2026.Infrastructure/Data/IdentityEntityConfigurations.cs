using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("user", "identity");
        builder.Property(entity => entity.DisplayName).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.RefreshTokenHash).HasMaxLength(64);
        builder.HasIndex(entity => entity.RefreshTokenHash).IsUnique().HasFilter("[refresh_token_hash] IS NOT NULL");
    }
}

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("role", "identity");
        builder.HasData(
            new ApplicationRole
            {
                Id = 1,
                Name = BookDocRoleNames.PlatformOperator,
                NormalizedName = BookDocRoleNames.PlatformOperator.ToUpperInvariant(),
                ConcurrencyStamp = "bookdoc-platform-operator-v1",
                IsPlatformRole = true,
                IsActive = true
            },
            new ApplicationRole
            {
                Id = 2,
                Name = BookDocRoleNames.ClinicAdministrator,
                NormalizedName = BookDocRoleNames.ClinicAdministrator.ToUpperInvariant(),
                ConcurrencyStamp = "bookdoc-clinic-administrator-v1",
                IsPlatformRole = false,
                IsActive = true
            });
    }
}

internal sealed class ApplicationUserScopeConfiguration : IEntityTypeConfiguration<ApplicationUserScope>
{
    public void Configure(EntityTypeBuilder<ApplicationUserScope> builder)
    {
        builder.ToTable("user_scope", "identity");
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => new { entity.UserId, entity.TenantId, entity.OrganizationId, entity.BranchId }).IsUnique();
        builder.HasIndex(entity => new { entity.UserId, entity.IsDefault }).HasFilter("[is_default] = 1").IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithMany(user => user.ScopeAssignments)
            .HasForeignKey(entity => entity.UserId);
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entity => entity.TenantId);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.OrganizationId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.OrganizationId, entity.BranchId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.OrganizationId, entity.Id });
    }
}

internal sealed class ApplicationIdentitySupportConfiguration :
    IEntityTypeConfiguration<ApplicationUserClaim>,
    IEntityTypeConfiguration<ApplicationUserRole>,
    IEntityTypeConfiguration<ApplicationUserLogin>,
    IEntityTypeConfiguration<ApplicationRoleClaim>,
    IEntityTypeConfiguration<ApplicationUserToken>
{
    public void Configure(EntityTypeBuilder<ApplicationUserClaim> builder) =>
        builder.ToTable("user_claim", "identity");

    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder) =>
        builder.ToTable("user_role", "identity");

    public void Configure(EntityTypeBuilder<ApplicationUserLogin> builder) =>
        builder.ToTable("user_login", "identity");

    public void Configure(EntityTypeBuilder<ApplicationUserToken> builder) =>
        builder.ToTable("user_token", "identity");

    public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder)
    {
        builder.ToTable("role_claim", "identity");
        var all = AllPermissions();
        var claims = new List<ApplicationRoleClaim>();
        var id = 1;
        foreach (var permission in all)
        {
            claims.Add(new ApplicationRoleClaim
            {
                Id = id++,
                RoleId = 1,
                ClaimType = BookDocClaimTypes.Permission,
                ClaimValue = permission
            });
        }

        foreach (var permission in FoundationPermissions.BranchAssignable.Order(StringComparer.Ordinal))
        {
            claims.Add(new ApplicationRoleClaim
            {
                Id = id++,
                RoleId = 2,
                ClaimType = BookDocClaimTypes.Permission,
                ClaimValue = permission
            });
        }

        builder.HasData(claims);
    }

    private static IReadOnlyCollection<string> AllPermissions() =>
        typeof(FoundationPermissions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
}
