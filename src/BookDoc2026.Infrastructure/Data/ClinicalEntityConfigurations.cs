using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class ClinicalEncounterConfiguration : IEntityTypeConfiguration<ClinicalEncounter>
{
    public void Configure(EntityTypeBuilder<ClinicalEncounter> builder)
    {
        builder.ToTable("encounter", "clinical", table =>
            table.HasCheckConstraint("ck_clinical_encounter_status", "[status] BETWEEN 1 AND 2"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.EncounterNumber).HasMaxLength(24).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.BookingId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.PatientId, item.Status });
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.SupervisingPractitionerId });
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Booking>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BookingId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Patient>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalService>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ServiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class EncounterRevisionConfiguration : IEntityTypeConfiguration<EncounterRevision>
{
    public void Configure(EntityTypeBuilder<EncounterRevision> builder)
    {
        builder.ToTable("encounter_revision", "clinical", table =>
            table.HasCheckConstraint("ck_encounter_revision_kind", "[kind] BETWEEN 1 AND 3"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Ignore(item => item.Content);
        builder.Property(item => item.SpecialtyCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.TemplateKey).HasMaxLength(80).IsRequired();
        builder.Property(item => item.TemplateVersion).HasMaxLength(40).IsRequired();
        builder.Property(item => item.ChiefComplaint).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.History).HasMaxLength(8000);
        builder.Property(item => item.Examination).HasMaxLength(8000);
        builder.Property(item => item.Assessment).HasMaxLength(8000);
        builder.Property(item => item.Plan).HasMaxLength(8000);
        builder.Property(item => item.Instructions).HasMaxLength(8000);
        builder.Property(item => item.BodySite).HasMaxLength(200);
        builder.Property(item => item.LateralityCode).HasMaxLength(30);
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(item => item.AmendmentReason).HasMaxLength(500);
        builder.HasIndex(item => new { item.TenantId, item.EncounterId, item.RevisionNumber }).IsUnique();
        builder.HasOne<ClinicalEncounter>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.EncounterId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<EncounterRevision>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ParentRevisionId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class PatientVitalSignsConfiguration : IEntityTypeConfiguration<PatientVitalSigns>
{
    public void Configure(EntityTypeBuilder<PatientVitalSigns> builder)
    {
        builder.ToTable("patient_vital_signs", "clinical");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.TemperatureF).HasPrecision(5, 2);
        builder.Property(item => item.WeightKg).HasPrecision(6, 2);
        builder.Property(item => item.HeightCm).HasPrecision(6, 2);
        builder.Property(item => item.Bmi).HasPrecision(5, 2);
        builder.Property(item => item.BloodGlucoseMgDl).HasPrecision(6, 2);
        builder.Property(item => item.RecordedByActorId).HasMaxLength(128);
        builder.Property(item => item.ClinicalNotes).HasMaxLength(1000);
        builder.Property(item => item.Version).IsConcurrencyToken();

        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.PatientId, item.RecordedUtc });
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.BookingId });
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.ClinicalEncounterId });

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });

        builder.HasOne<Patient>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}
