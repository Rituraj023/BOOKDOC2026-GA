using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Stakeholders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class StakeholderConfiguration : IEntityTypeConfiguration<Stakeholder>
{
    public void Configure(EntityTypeBuilder<Stakeholder> builder)
    {
        builder.ToTable("stakeholder", "stakeholder", table =>
        {
            table.HasCheckConstraint("ck_stakeholder_type", "[type] BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_stakeholder_status", "[status] BETWEEN 1 AND 2");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.DisplayName).HasMaxLength(240).IsRequired();
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.TenantId, entity.Type, entity.Status, entity.DisplayName });
    }
}

internal sealed class StakeholderPersonConfiguration : IEntityTypeConfiguration<StakeholderPerson>
{
    public void Configure(EntityTypeBuilder<StakeholderPerson> builder)
    {
        builder.ToTable("person", "stakeholder", table =>
            table.HasCheckConstraint("ck_stakeholder_person_sex", "[administrative_sex] BETWEEN 0 AND 3"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Honorific).HasMaxLength(30);
        builder.Property(entity => entity.GivenName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.MiddleName).HasMaxLength(100);
        builder.Property(entity => entity.FamilyName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NormalizedSearchName).HasMaxLength(302).IsRequired();
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.NormalizedSearchName, entity.DateOfBirth });
        builder.HasOne<Stakeholder>()
            .WithOne()
            .HasForeignKey<StakeholderPerson>(entity => new { entity.TenantId, entity.StakeholderId })
            .HasPrincipalKey<Stakeholder>(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class StakeholderCorporateConfiguration : IEntityTypeConfiguration<StakeholderCorporate>
{
    public void Configure(EntityTypeBuilder<StakeholderCorporate> builder)
    {
        builder.ToTable("corporate", "stakeholder");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.LegalName).HasMaxLength(240).IsRequired();
        builder.Property(entity => entity.TradeName).HasMaxLength(240);
        builder.Property(entity => entity.RegistrationNumber).HasMaxLength(100);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.RegistrationNumber });
        builder.HasOne<Stakeholder>()
            .WithOne()
            .HasForeignKey<StakeholderCorporate>(entity => new { entity.TenantId, entity.StakeholderId })
            .HasPrincipalKey<Stakeholder>(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class StakeholderContactPointConfiguration : IEntityTypeConfiguration<StakeholderContactPoint>
{
    public void Configure(EntityTypeBuilder<StakeholderContactPoint> builder)
    {
        builder.ToTable("contact_point", "stakeholder", table =>
            table.HasCheckConstraint("ck_stakeholder_contact_type", "[type] BETWEEN 1 AND 2"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Value).HasMaxLength(320).IsRequired();
        builder.Property(entity => entity.NormalizedValue).HasMaxLength(320).IsRequired();
        builder.HasIndex(entity => new { entity.TenantId, entity.NormalizedValue });
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId, entity.Type, entity.NormalizedValue }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId, entity.Type })
            .IsUnique()
            .HasFilter("[is_primary] = 1");
        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.StakeholderId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class StakeholderIdentifierConfiguration : IEntityTypeConfiguration<StakeholderIdentifier>
{
    public void Configure(EntityTypeBuilder<StakeholderIdentifier> builder)
    {
        builder.ToTable("identifier", "stakeholder");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Type).HasMaxLength(60).IsRequired();
        builder.Property(entity => entity.Value).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NormalizedValue).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Issuer).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => new
        {
            entity.TenantId,
            entity.Type,
            entity.Issuer,
            entity.NormalizedValue
        }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId, entity.IsActive });
        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.StakeholderId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class StakeholderAddressConfiguration : IEntityTypeConfiguration<StakeholderAddress>
{
    public void Configure(EntityTypeBuilder<StakeholderAddress> builder)
    {
        builder.ToTable("address", "stakeholder", table =>
            table.HasCheckConstraint(
                "ck_stakeholder_address_india_postal_code",
                "[country_code] = 'IN' AND LEN([postal_code]) = 6 AND [postal_code] NOT LIKE '%[^0-9]%'"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.AddressType).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Line1).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Line2).HasMaxLength(200);
        builder.Property(entity => entity.City).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.StateCode).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.PostalCode).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.CountryCode).HasMaxLength(2).IsRequired();
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId, entity.AddressType }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId })
            .IsUnique()
            .HasFilter("[is_primary] = 1");
        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.StakeholderId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class StakeholderDocumentReferenceConfiguration : IEntityTypeConfiguration<StakeholderDocumentReference>
{
    public void Configure(EntityTypeBuilder<StakeholderDocumentReference> builder)
    {
        builder.ToTable("document_reference", "stakeholder");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ReferenceNumber).HasMaxLength(200);
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId, entity.DocumentTypeId, entity.FileId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.FileId }).IsUnique();
        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.StakeholderId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patient", "patient", table =>
            table.HasCheckConstraint("ck_patient_status", "[status] BETWEEN 1 AND 5"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.PatientNumber).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.RegistrationPayloadHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(entity => entity.BloodGroup).HasMaxLength(4);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.TenantId, entity.PatientNumber }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.RegistrationRequestId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.StakeholderId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.RegistrationBranchId, entity.CreatedUtc });
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.RegistrationBranchId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
        builder.HasOne<Stakeholder>()
            .WithOne()
            .HasForeignKey<Patient>(entity => new { entity.TenantId, entity.StakeholderId })
            .HasPrincipalKey<Stakeholder>(entity => new { entity.TenantId, entity.Id });
    }
}
internal sealed class PatientRelationConfiguration : IEntityTypeConfiguration<PatientRelation>
{
    public void Configure(EntityTypeBuilder<PatientRelation> builder)
    {
        builder.ToTable("patient_relation", "patient", table =>
            table.HasCheckConstraint("ck_patient_relation_not_self", "[patient_id] <> [related_patient_id]"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Notes).HasMaxLength(500);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.TenantId, entity.PatientId, entity.RelatedPatientId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.RelatedPatientId });
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.PatientId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.RelatedPatientId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
