using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class ClinicalServiceConfiguration : IEntityTypeConfiguration<ClinicalService>
{
    public void Configure(EntityTypeBuilder<ClinicalService> builder)
    {
        builder.ToTable("service", "catalog", table =>
        {
            table.HasCheckConstraint("ck_catalog_service_status", "[status] BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_catalog_service_duration", "[default_duration_minutes] BETWEEN 5 AND 1440");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.Code).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.Status, entity.Name });
    }
}

internal sealed class ResourceCategoryConfiguration : IEntityTypeConfiguration<ResourceCategory>
{
    public void Configure(EntityTypeBuilder<ResourceCategory> builder)
    {
        builder.ToTable("resource_category", "resource", table =>
            table.HasCheckConstraint("ck_resource_category_kind", "[kind] BETWEEN 1 AND 7"));
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.Code).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.Kind, entity.IsActive });
        builder.HasOne<ResourceCategory>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.ParentCategoryId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class BookableResourceConfiguration : IEntityTypeConfiguration<BookableResource>
{
    public void Configure(EntityTypeBuilder<BookableResource> builder)
    {
        builder.ToTable("bookable_resource", "resource", table =>
        {
            table.HasCheckConstraint("ck_bookable_resource_capacity_mode", "[capacity_mode] BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_bookable_resource_capacity", "[capacity] BETWEEN 1 AND 1000");
            table.HasCheckConstraint(
                "ck_bookable_resource_exclusive_capacity",
                "[capacity_mode] <> 1 OR [capacity] = 1");
            table.HasCheckConstraint("ck_bookable_resource_status", "[operational_status] BETWEEN 1 AND 5");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.BranchId, entity.Id });
        builder.Property(entity => entity.Code).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.TenantId, entity.BranchId, entity.Code }).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.TenantId,
            entity.BranchId,
            entity.CategoryId,
            entity.IsActive,
            entity.OperationalStatus
        });
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.BranchId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
        builder.HasOne<ResourceCategory>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.CategoryId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class ResourceCapabilityConfiguration : IEntityTypeConfiguration<ResourceCapability>
{
    public void Configure(EntityTypeBuilder<ResourceCapability> builder)
    {
        builder.ToTable("resource_capability", "resource", table =>
        {
            table.HasCheckConstraint(
                "ck_resource_capability_duration",
                "[duration_override_minutes] IS NULL OR [duration_override_minutes] BETWEEN 5 AND 1440");
            table.HasCheckConstraint("ck_resource_capability_capacity", "[capacity_required] BETWEEN 1 AND 1000");
        });
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => new { entity.TenantId, entity.ResourceId, entity.ServiceId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.ServiceId, entity.IsActive });
        builder.HasOne<BookableResource>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.ResourceId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
        builder.HasOne<ClinicalService>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.ServiceId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class ServiceResourceRequirementConfiguration : IEntityTypeConfiguration<ServiceResourceRequirement>
{
    public void Configure(EntityTypeBuilder<ServiceResourceRequirement> builder)
    {
        builder.ToTable("service_resource_requirement", "catalog", table =>
            table.HasCheckConstraint("ck_service_resource_requirement_quantity", "[quantity] BETWEEN 1 AND 1000"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.RoleCode).HasMaxLength(40).IsRequired();
        builder.HasIndex(entity => new
        {
            entity.TenantId,
            entity.ServiceId,
            entity.CategoryId,
            entity.RoleCode
        }).IsUnique();
        builder.HasOne<ClinicalService>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.ServiceId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
        builder.HasOne<ResourceCategory>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.CategoryId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class ResourceStatusEventConfiguration : IEntityTypeConfiguration<ResourceStatusEvent>
{
    public void Configure(EntityTypeBuilder<ResourceStatusEvent> builder)
    {
        builder.ToTable("resource_status_event", "resource", table =>
        {
            table.HasCheckConstraint("ck_resource_status_event_from", "[from_status] BETWEEN 1 AND 5");
            table.HasCheckConstraint("ck_resource_status_event_to", "[to_status] BETWEEN 1 AND 5");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(entity => new { entity.TenantId, entity.ResourceId, entity.CreatedUtc });
        builder.HasOne<BookableResource>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.BranchId, entity.ResourceId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.BranchId, entity.Id });
    }
}
