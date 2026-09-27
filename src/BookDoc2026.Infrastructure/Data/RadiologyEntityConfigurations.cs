using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Radiology;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class RadiologyStudyConfiguration : IEntityTypeConfiguration<RadiologyStudy>
{
    public void Configure(EntityTypeBuilder<RadiologyStudy> builder)
    {
        builder.ToTable("study", "radiology", table =>
        {
            table.HasCheckConstraint("ck_radiology_study_modality", "[modality] BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_radiology_study_status", "[status] BETWEEN 1 AND 7");
            table.HasCheckConstraint("ck_radiology_study_attempt_count", "[acquisition_attempt_count] >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.OrderId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.RegistrationRequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.Status, item.RegisteredUtc });
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<InvestigationOrder>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.OrderId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Patient>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalService>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ServiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class RadiologyAcquisitionAttemptConfiguration :
    IEntityTypeConfiguration<RadiologyAcquisitionAttempt>
{
    public void Configure(EntityTypeBuilder<RadiologyAcquisitionAttempt> builder)
    {
        builder.ToTable("acquisition_attempt", "radiology", table =>
        {
            table.HasCheckConstraint("ck_radiology_acquisition_sequence", "[sequence] > 0");
            table.HasCheckConstraint("ck_radiology_acquisition_outcome", "[outcome] BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_radiology_acquisition_timing", "[completed_utc] >= [started_utc]");
            table.HasCheckConstraint("ck_radiology_acquisition_deviation",
                "([has_protocol_deviation] = 1 AND [deviation_code] IS NOT NULL) OR ([has_protocol_deviation] = 0 AND [deviation_code] IS NULL AND [deviation_note] IS NULL)");
            table.HasCheckConstraint("ck_radiology_acquisition_abort_reason",
                "([outcome] = 2 AND [outcome_reason_code] IS NOT NULL) OR ([outcome] = 1 AND [outcome_reason_code] IS NULL AND [outcome_note] IS NULL)");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.ProtocolCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.ProtocolVersion).HasMaxLength(40).IsRequired();
        builder.Property(item => item.DeviationCode).HasMaxLength(40);
        builder.Property(item => item.DeviationNote).HasMaxLength(500);
        builder.Property(item => item.OutcomeReasonCode).HasMaxLength(40);
        builder.Property(item => item.OutcomeNote).HasMaxLength(500);
        builder.Property(item => item.ExternalStudyReference).HasMaxLength(200);
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.StudyId, item.Sequence }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.ExternalStudyReference }).IsUnique()
            .HasFilter("[external_study_reference] IS NOT NULL");
        builder.HasOne<RadiologyStudy>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.StudyId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<BookableResource>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.EquipmentResourceId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
    }
}

internal sealed class RadiologyQualityReviewConfiguration : IEntityTypeConfiguration<RadiologyQualityReview>
{
    public void Configure(EntityTypeBuilder<RadiologyQualityReview> builder)
    {
        builder.ToTable("quality_review", "radiology", table =>
            table.HasCheckConstraint("ck_radiology_quality_decision", "[decision] BETWEEN 1 AND 2"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.ReasonCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Note).HasMaxLength(500);
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.AcquisitionAttemptId }).IsUnique();
        builder.HasOne<RadiologyStudy>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.StudyId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<RadiologyAcquisitionAttempt>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.AcquisitionAttemptId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class RadiologyStudyEventConfiguration : IEntityTypeConfiguration<RadiologyStudyEvent>
{
    public void Configure(EntityTypeBuilder<RadiologyStudyEvent> builder)
    {
        builder.ToTable("study_event", "radiology");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.RequestFingerprint).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(item => item.Action).HasMaxLength(40).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.StudyId, item.StudyVersion }).IsUnique();
        builder.HasOne<RadiologyStudy>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.StudyId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}
