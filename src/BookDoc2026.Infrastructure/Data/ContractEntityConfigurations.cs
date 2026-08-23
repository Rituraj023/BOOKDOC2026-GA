using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Contracts;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class ContractAgreementConfiguration : IEntityTypeConfiguration<ContractAgreement>
{
    public void Configure(EntityTypeBuilder<ContractAgreement> builder)
    {
        builder.ToTable("contract", "contract", table =>
            table.HasCheckConstraint("ck_contract_status", "[status] BETWEEN 1 AND 4"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.ContractNumber).HasMaxLength(40).IsRequired();
        builder.Property(item => item.ContractTypeCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.PackagePrice).HasPrecision(19, 2);
        builder.Property(item => item.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(item => item.RuleVersion).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(1000);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.ContractNumber }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.PatientId, item.Status, item.ValidTo });
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Patient>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class ContractEntitlementConfiguration : IEntityTypeConfiguration<ContractEntitlement>
{
    public void Configure(EntityTypeBuilder<ContractEntitlement> builder)
    {
        builder.ToTable("entitlement", "contract", table =>
        {
            table.HasCheckConstraint("ck_contract_entitlement_units",
                "[total_units] >= 1 AND [reserved_units] >= 0 AND [consumed_units] >= 0 AND [reserved_units] + [consumed_units] <= [total_units]");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Ignore(item => item.AvailableUnits);
        builder.Property(item => item.UnitPrice).HasPrecision(19, 2);
        builder.Property(item => item.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(item => item.RuleVersion).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.ContractId, item.ServiceId, item.ResourceCategoryId })
            .IsUnique().HasFilter("[resource_category_id] IS NOT NULL");
        builder.HasIndex(item => new { item.TenantId, item.ContractId, item.ServiceId })
            .IsUnique().HasFilter("[resource_category_id] IS NULL");
        builder.HasOne<ContractAgreement>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ContractId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalService>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ServiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ResourceCategory>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ResourceCategoryId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class EntitlementReservationConfiguration : IEntityTypeConfiguration<EntitlementReservation>
{
    public void Configure(EntityTypeBuilder<EntitlementReservation> builder)
    {
        builder.ToTable("entitlement_reservation", "contract", table =>
            table.HasCheckConstraint("ck_entitlement_reservation_status", "[status] BETWEEN 1 AND 3"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(item => item.ReleaseReason).HasMaxLength(250);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.EntitlementId, item.BookingId }).IsUnique();
        builder.HasOne<ContractAgreement>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ContractId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ContractEntitlement>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.EntitlementId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Booking>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BookingId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}
