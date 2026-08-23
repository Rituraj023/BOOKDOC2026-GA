using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Domain.Stakeholders;
using BookDoc2026.Domain.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class PractitionerProfileConfiguration : IEntityTypeConfiguration<PractitionerProfile>
{
    public void Configure(EntityTypeBuilder<PractitionerProfile> builder)
    {
        builder.ToTable("practitioner", "workforce", table =>
            table.HasCheckConstraint("ck_workforce_practitioner_status", "[status] BETWEEN 1 AND 4"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.PractitionerCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.PractitionerTypeCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.PractitionerCode }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.StakeholderId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.IdentitySubjectId }).IsUnique();
        builder.HasOne<Stakeholder>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.StakeholderId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(item => item.IdentitySubjectId)
            .HasPrincipalKey(item => item.Id);
    }
}

internal sealed class PractitionerCredentialConfiguration : IEntityTypeConfiguration<PractitionerCredential>
{
    public void Configure(EntityTypeBuilder<PractitionerCredential> builder)
    {
        builder.ToTable("practitioner_credential", "workforce", table =>
            table.HasCheckConstraint("ck_workforce_credential_status", "[verification_status] BETWEEN 1 AND 3"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.CredentialTypeCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.RegistrationNumber).HasMaxLength(100).IsRequired();
        builder.Property(item => item.IssuingAuthority).HasMaxLength(160).IsRequired();
        builder.Property(item => item.DecisionReason).HasMaxLength(500);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.CredentialTypeCode, item.IssuingAuthority,
            item.RegistrationNumber }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.PractitionerId, item.VerificationStatus, item.ValidTo });
        builder.HasOne<PractitionerProfile>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.PractitionerId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class PractitionerAssignmentConfiguration : IEntityTypeConfiguration<PractitionerAssignment>
{
    public void Configure(EntityTypeBuilder<PractitionerAssignment> builder)
    {
        builder.ToTable("practitioner_assignment", "workforce", table =>
            table.HasCheckConstraint("ck_workforce_assignment_status", "[status] BETWEEN 1 AND 3"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.RoleCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.PractitionerId, item.BranchId, item.ServiceId,
            item.Status, item.EffectiveFrom });
        builder.HasOne<PractitionerProfile>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.PractitionerId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalService>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ServiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<BookableResource>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.BookableResourceId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
    }
}
