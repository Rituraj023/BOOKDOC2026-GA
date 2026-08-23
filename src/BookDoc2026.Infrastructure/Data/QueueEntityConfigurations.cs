using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class ImagingServicePointConfiguration : IEntityTypeConfiguration<ImagingServicePoint>
{
    public void Configure(EntityTypeBuilder<ImagingServicePoint> builder)
    {
        builder.ToTable("imaging_service_point", "queue", table =>
            table.HasCheckConstraint("ck_imaging_service_point_modality", "[modality] BETWEEN 1 AND 2"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.Code).HasMaxLength(20).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.Code }).IsUnique();
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<BookableResource>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.ResourceId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
    }
}

internal sealed class QueueTicketConfiguration : IEntityTypeConfiguration<QueueTicket>
{
    public void Configure(EntityTypeBuilder<QueueTicket> builder)
    {
        builder.ToTable("ticket", "queue", table =>
        {
            table.HasCheckConstraint("ck_queue_ticket_priority", "[priority] BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_queue_ticket_status", "[status] BETWEEN 1 AND 6");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.Property(item => item.DisplayToken).HasMaxLength(16).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.DisplayToken }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.ServicePointId, item.Status, item.Priority, item.ArrivedUtc });
        builder.HasIndex(item => new { item.TenantId, item.ServicePointId, item.BookingId })
            .IsUnique().HasFilter("[booking_id] IS NOT NULL AND [status] >= 1 AND [status] <= 4");
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ImagingServicePoint>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ServicePointId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Patient>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Booking>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BookingId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class QueueTicketEventConfiguration : IEntityTypeConfiguration<QueueTicketEvent>
{
    public void Configure(EntityTypeBuilder<QueueTicketEvent> builder)
    {
        builder.ToTable("ticket_event", "queue");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Action).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(250);
        builder.HasIndex(item => new { item.TenantId, item.TicketId, item.TicketVersion }).IsUnique();
        builder.HasOne<QueueTicket>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.TicketId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}
