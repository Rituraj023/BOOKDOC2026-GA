using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("availability_rule", "scheduling", table =>
        {
            table.HasCheckConstraint("ck_availability_rule_interval", "[local_start] < [local_end]");
            table.HasCheckConstraint("ck_availability_rule_capacity", "[capacity] BETWEEN 1 AND 1000");
            table.HasCheckConstraint("ck_availability_rule_slot", "[slot_interval_minutes] BETWEEN 5 AND 1440");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocalStart).HasColumnType("time");
        builder.Property(x => x.LocalEnd).HasColumnType("time");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.BranchId, x.ResourceId, x.DayOfWeek, x.IsActive });
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => new { x.TenantId, x.BranchId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        builder.HasOne<BookableResource>().WithMany().HasForeignKey(x => new { x.TenantId, x.BranchId, x.ResourceId }).HasPrincipalKey(x => new { x.TenantId, x.BranchId, x.Id });
        builder.HasOne<ClinicalService>().WithMany().HasForeignKey(x => new { x.TenantId, x.ServiceId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
    }
}

internal sealed class AvailabilityExceptionConfiguration : IEntityTypeConfiguration<AvailabilityException>
{
    public void Configure(EntityTypeBuilder<AvailabilityException> builder)
    {
        builder.ToTable("availability_exception", "scheduling", table =>
        {
            table.HasCheckConstraint("ck_availability_exception_interval", "[start_utc] < [end_utc]");
            table.HasCheckConstraint("ck_availability_exception_kind", "[kind] BETWEEN 1 AND 2");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.BranchId, x.ResourceId, x.StartUtc, x.EndUtc });
        builder.HasOne<BookableResource>().WithMany().HasForeignKey(x => new { x.TenantId, x.BranchId, x.ResourceId }).HasPrincipalKey(x => new { x.TenantId, x.BranchId, x.Id });
    }
}

internal sealed class SchedulingHoldConfiguration : IEntityTypeConfiguration<SchedulingHold>
{
    public void Configure(EntityTypeBuilder<SchedulingHold> builder)
    {
        builder.ToTable("hold", "scheduling", table =>
        {
            table.HasCheckConstraint("ck_scheduling_hold_interval", "[start_utc] < [end_utc]");
            table.HasCheckConstraint("ck_scheduling_hold_status", "[status] BETWEEN 1 AND 4");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.TenantId, x.Id });
        builder.Property(x => x.PayloadHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.BranchId, x.Status, x.ExpiresUtc });
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => new { x.TenantId, x.BranchId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        builder.HasOne<Patient>().WithMany().HasForeignKey(x => new { x.TenantId, x.PatientId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        builder.HasOne<ClinicalService>().WithMany().HasForeignKey(x => new { x.TenantId, x.ServiceId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
    }
}

internal sealed class ResourceReservationConfiguration : IEntityTypeConfiguration<ResourceReservation>
{
    public void Configure(EntityTypeBuilder<ResourceReservation> builder)
    {
        builder.ToTable("resource_reservation", "scheduling", table =>
        {
            table.HasCheckConstraint("ck_resource_reservation_interval", "[start_utc] < [end_utc]");
            table.HasCheckConstraint("ck_resource_reservation_quantity", "[quantity] BETWEEN 1 AND 1000");
        });
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.HoldId, x.ResourceId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.ResourceId, x.StartUtc, x.EndUtc });
        builder.HasOne<SchedulingHold>().WithMany().HasForeignKey(x => new { x.TenantId, x.HoldId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        builder.HasOne<BookableResource>().WithMany().HasForeignKey(x => new { x.TenantId, x.BranchId, x.ResourceId }).HasPrincipalKey(x => new { x.TenantId, x.BranchId, x.Id });
    }
}
