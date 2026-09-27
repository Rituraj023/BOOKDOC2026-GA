using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class InvestigationOrderConfiguration : IEntityTypeConfiguration<InvestigationOrder>
{
    public void Configure(EntityTypeBuilder<InvestigationOrder> builder)
    {
        builder.ToTable("investigation_order", "clinical", table =>
        {
            table.HasCheckConstraint("ck_investigation_order_status", "[status] = 1");
            table.HasCheckConstraint("ck_investigation_result_status", "[result_status] = 1");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.OrderNumber).HasMaxLength(20).IsRequired();
        builder.Property(item => item.ClinicalIndication).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.OrderNumber }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.EncounterId, item.OrderedUtc });
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalEncounter>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.EncounterId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Patient>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalService>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.RequestedServiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class InvestigationOrderEventConfiguration : IEntityTypeConfiguration<InvestigationOrderEvent>
{
    public void Configure(EntityTypeBuilder<InvestigationOrderEvent> builder)
    {
        builder.ToTable("investigation_order_event", "clinical");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.Action).HasMaxLength(40).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.OrderId, item.OrderVersion }).IsUnique();
        builder.HasOne<InvestigationOrder>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.OrderId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<QueueTicket>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.QueueTicketId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}
