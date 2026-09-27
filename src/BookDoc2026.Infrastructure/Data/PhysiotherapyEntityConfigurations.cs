using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class PhysiotherapyCarePlanConfiguration : IEntityTypeConfiguration<PhysiotherapyCarePlan>
{
    public void Configure(EntityTypeBuilder<PhysiotherapyCarePlan> builder)
    {
        builder.ToTable("physiotherapy_care_plan", "clinical", table =>
            table.HasCheckConstraint("ck_physio_care_plan_status", "[status] BETWEEN 1 AND 4"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.HasAlternateKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.Property(item => item.CarePlanNumber).HasMaxLength(40).IsRequired();
        builder.Property(item => item.ClosureReason).HasMaxLength(1000);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.InitialEncounterId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.PatientId, item.Status });
        builder.HasOne<Branch>().WithMany().HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Patient>().WithMany().HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalEncounter>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.InitialEncounterId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalService>().WithMany().HasForeignKey(item => new { item.TenantId, item.ServiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class PhysiotherapyCarePlanRevisionConfiguration
    : IEntityTypeConfiguration<PhysiotherapyCarePlanRevision>
{
    public void Configure(EntityTypeBuilder<PhysiotherapyCarePlanRevision> builder)
    {
        builder.ToTable("physiotherapy_care_plan_revision", "clinical");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.HasAlternateKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.Ignore(item => item.Content);
        builder.Property(item => item.GoalSummary).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.FrequencyAndDuration).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.PlannedInterventions).HasMaxLength(6000).IsRequired();
        builder.Property(item => item.Precautions).HasMaxLength(4000);
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(item => item.ChangeReason).HasMaxLength(500);
        builder.HasIndex(item => new { item.TenantId, item.CarePlanId, item.RevisionNumber }).IsUnique();
        builder.HasOne<PhysiotherapyCarePlan>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.CarePlanId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.HasOne<PhysiotherapyCarePlanRevision>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.ParentRevisionId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
    }
}

internal sealed class PhysiotherapyTreatmentSessionConfiguration
    : IEntityTypeConfiguration<PhysiotherapyTreatmentSession>
{
    public void Configure(EntityTypeBuilder<PhysiotherapyTreatmentSession> builder)
    {
        builder.ToTable("physiotherapy_treatment_session", "clinical", table =>
            table.HasCheckConstraint("ck_physio_session_sequence", "[sequence_number] > 0"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.HasAlternateKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.Property(item => item.SubjectiveResponse).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.Interventions).HasMaxLength(8000).IsRequired();
        builder.Property(item => item.Tolerance).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.NextPlan).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.AdverseEventDetails).HasMaxLength(4000);
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.EncounterId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.CarePlanId, item.SequenceNumber }).IsUnique();
        builder.HasOne<PhysiotherapyCarePlan>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.CarePlanId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.HasOne<ClinicalEncounter>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.EncounterId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Booking>().WithMany().HasForeignKey(item => new { item.TenantId, item.BookingId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class PhysiotherapyOutcomeObservationConfiguration
    : IEntityTypeConfiguration<PhysiotherapyOutcomeObservation>
{
    public void Configure(EntityTypeBuilder<PhysiotherapyOutcomeObservation> builder)
    {
        builder.ToTable("physiotherapy_outcome_observation", "clinical");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ContextCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.MeasureCode).HasMaxLength(80).IsRequired();
        builder.Property(item => item.ToolVersion).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Value).HasPrecision(19, 4);
        builder.Property(item => item.Unit).HasMaxLength(30).IsRequired();
        builder.Property(item => item.BodySite).HasMaxLength(200);
        builder.Property(item => item.LateralityCode).HasMaxLength(30);
        builder.HasIndex(item => new { item.TenantId, item.CarePlanId, item.MeasureCode,
            item.ToolVersion, item.ObservedUtc });
        builder.HasOne<PhysiotherapyCarePlan>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.CarePlanId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.HasOne<PhysiotherapyTreatmentSession>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.TreatmentSessionId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
    }
}
