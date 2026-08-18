using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.ToTable("message_template", "communication", table =>
        {
            table.HasCheckConstraint("ck_message_template_version", "[template_version] > 0");
            table.HasCheckConstraint("ck_message_template_scope", "[branch_id] IS NULL OR [organization_id] IS NOT NULL");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Key).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Culture).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.SubjectTemplate).HasMaxLength(1000);
        builder.Property(entity => entity.BodyTemplate).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Revision).IsConcurrencyToken();
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.HasIndex(entity => new
        {
            entity.TenantId,
            entity.Key,
            entity.Channel,
            entity.Culture,
            entity.TemplateVersion
        })
            .HasDatabaseName("ux_message_template_tenant_scope")
            .IsUnique()
            .HasFilter("[organization_id] IS NULL AND [branch_id] IS NULL");
        builder.HasIndex(entity => new
        {
            entity.TenantId,
            entity.OrganizationId,
            entity.Key,
            entity.Channel,
            entity.Culture,
            entity.TemplateVersion
        })
            .HasDatabaseName("ux_message_template_organization_scope")
            .IsUnique()
            .HasFilter("[organization_id] IS NOT NULL AND [branch_id] IS NULL");
        builder.HasIndex(entity => new
        {
            entity.TenantId,
            entity.OrganizationId,
            entity.BranchId,
            entity.Key,
            entity.Channel,
            entity.Culture,
            entity.TemplateVersion
        })
            .HasDatabaseName("ux_message_template_branch_scope")
            .IsUnique()
            .HasFilter("[organization_id] IS NOT NULL AND [branch_id] IS NOT NULL");
        builder.HasIndex(entity => new { entity.TenantId, entity.Key, entity.Channel, entity.Culture, entity.Status });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entity => entity.TenantId);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.OrganizationId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.OrganizationId, entity.BranchId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.OrganizationId, entity.Id });
    }
}

internal sealed class MessageDeliveryAttemptConfiguration : IEntityTypeConfiguration<MessageDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<MessageDeliveryAttempt> builder)
    {
        builder.ToTable("message_delivery_attempt", "communication", table =>
            table.HasCheckConstraint("ck_message_delivery_attempt_number", "[attempt_number] > 0"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.TemplateKey).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.ProviderCode).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.RecipientHint).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.ProviderMessageId).HasMaxLength(200);
        builder.Property(entity => entity.ErrorCode).HasMaxLength(120);
        builder.HasIndex(entity => new { entity.TenantId, entity.OperationId, entity.Channel, entity.AttemptNumber }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.BranchId, entity.CreatedUtc });
        builder.HasIndex(entity => new { entity.TenantId, entity.Status, entity.CreatedUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entity => entity.TenantId);
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.BranchId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class CommunicationPreferenceEventConfiguration : IEntityTypeConfiguration<CommunicationPreferenceEvent>
{
    public void Configure(EntityTypeBuilder<CommunicationPreferenceEvent> builder)
    {
        builder.ToTable("communication_preference_event", "communication");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.PurposeCode).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.EvidenceSource).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.EvidenceReference).HasMaxLength(200);
        builder.HasIndex(entity => new
        {
            entity.TenantId,
            entity.StakeholderId,
            entity.PurposeCode,
            entity.Channel,
            entity.CreatedUtc
        });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entity => entity.TenantId);
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.BranchId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
        builder.HasOne<BookDoc2026.Domain.Stakeholders.Stakeholder>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.StakeholderId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id });
    }
}

internal sealed class ProviderCallbackInboxConfiguration : IEntityTypeConfiguration<ProviderCallbackInbox>
{
    public void Configure(EntityTypeBuilder<ProviderCallbackInbox> builder)
    {
        builder.ToTable("provider_callback_inbox", "communication");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ProviderCode).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.ExternalEventId).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ProviderMessageId).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.SignatureKeyId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.PayloadSha256).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ErrorCode).HasMaxLength(120);
        builder.HasIndex(entity => new { entity.ProviderCode, entity.ExternalEventId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.BranchId, entity.CreatedUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entity => entity.TenantId);
        builder.HasOne<MessageDeliveryAttempt>()
            .WithMany()
            .HasForeignKey(entity => entity.DeliveryAttemptId);
    }
}

internal sealed class MessageDeliveryStatusEventConfiguration : IEntityTypeConfiguration<MessageDeliveryStatusEvent>
{
    public void Configure(EntityTypeBuilder<MessageDeliveryStatusEvent> builder)
    {
        builder.ToTable("message_delivery_status_event", "communication");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ProviderCode).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.ExternalEventId).HasMaxLength(200).IsRequired();
        builder.HasIndex(entity => new { entity.ProviderCode, entity.ExternalEventId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.BranchId, entity.CreatedUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entity => entity.TenantId);
        builder.HasOne<ProviderCallbackInbox>()
            .WithMany()
            .HasForeignKey(entity => entity.CallbackInboxId);
        builder.HasOne<MessageDeliveryAttempt>()
            .WithMany()
            .HasForeignKey(entity => entity.DeliveryAttemptId);
    }
}
