using BookDoc2026.Domain.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class TenantApplicationConfiguration : IEntityTypeConfiguration<TenantApplication>
{
    public void Configure(EntityTypeBuilder<TenantApplication> builder)
    {
        builder.ToTable("tenant_application", "platform");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Slug).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.ContactEmail).HasMaxLength(320).IsRequired();
        builder.Property(entity => entity.FirstBranchName).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.FirstBranchCode).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.SubmissionSource).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => new { entity.Slug, entity.Status });
    }
}

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenant", "platform");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Slug).HasMaxLength(80).IsRequired();
        builder.HasIndex(entity => entity.Slug).IsUnique();
    }
}

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organization", "org");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.HasIndex(entity => new { entity.TenantId, entity.Name });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entity => entity.TenantId);
    }
}

internal sealed class BranchConfigurationMapping : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branch", "org");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Code).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.OrganizationId, entity.Id });
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique();
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.OrganizationId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class BranchConfigurationConfiguration : IEntityTypeConfiguration<BranchConfiguration>
{
    public void Configure(EntityTypeBuilder<BranchConfiguration> builder)
    {
        builder.ToTable("branch_configuration", "org");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.LogoUrl).HasMaxLength(500);
        builder.Property(entity => entity.InvoicePrefix).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.ReceiptPrefix).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.EmailSender).HasMaxLength(320);
        builder.Property(entity => entity.WhatsAppNumber).HasMaxLength(30);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => entity.BranchId).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.InvoicePrefix }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.ReceiptPrefix }).IsUnique();
        builder.HasOne<Branch>()
            .WithOne()
            .HasForeignKey<BranchConfiguration>(entity => new { entity.TenantId, entity.BranchId })
            .HasPrincipalKey<Branch>(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_event", "audit");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Action).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.EntityType).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.DataJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.CorrelationId).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => new { entity.TenantId, entity.CreatedUtc });
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId });
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_message", "worker");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.MessageType).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.LeaseOwner).HasMaxLength(160);
        builder.Property(entity => entity.LastErrorCode).HasMaxLength(120);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasIndex(entity => entity.OperationId).IsUnique();
        builder.HasIndex(entity => new { entity.Status, entity.NextAttemptUtc });
        builder.HasIndex(entity => new { entity.LeaseUntilUtc, entity.Status });
    }
}
