using BookDoc2026.Domain.Billing;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Contracts;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookDoc2026.Infrastructure.Data;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoice", "billing", table =>
        {
            table.HasCheckConstraint("ck_billing_invoice_status", "[status] BETWEEN 1 AND 3");
            table.HasCheckConstraint("ck_billing_invoice_amounts",
                "[subtotal] > 0 AND [discount_total] >= 0 AND [tax_total] >= 0 AND [total] > 0 AND [allocated_amount] >= 0 AND [allocated_amount] <= [total]");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.HasAlternateKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.Ignore(item => item.Balance);
        builder.Property(item => item.InvoiceNumber).HasMaxLength(60).IsRequired();
        builder.Property(item => item.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(item => item.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(item => item.Subtotal).HasPrecision(19, 2);
        builder.Property(item => item.DiscountTotal).HasPrecision(19, 2);
        builder.Property(item => item.TaxTotal).HasPrecision(19, 2);
        builder.Property(item => item.Total).HasPrecision(19, 2);
        builder.Property(item => item.AllocatedAmount).HasPrecision(19, 2);
        builder.Property(item => item.CalculationPolicyVersion).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(1000);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.InvoiceNumber }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.PatientId, item.Status, item.IssuedUtc });
        builder.HasOne<Branch>().WithMany().HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Patient>().WithMany().HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Booking>().WithMany().HasForeignKey(item => new { item.TenantId, item.BookingId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ContractAgreement>().WithMany().HasForeignKey(item => new { item.TenantId, item.ContractId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_line", "billing", table => table.HasCheckConstraint("ck_billing_invoice_line",
            "[quantity] BETWEEN 1 AND 1000 AND [unit_price] >= 0 AND [line_total] >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ServiceCodeSnapshot).HasMaxLength(40).IsRequired();
        builder.Property(item => item.DescriptionSnapshot).HasMaxLength(300).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(19, 2);
        builder.Property(item => item.LineTotal).HasPrecision(19, 2);
        builder.HasIndex(item => new { item.TenantId, item.InvoiceId });
        builder.HasOne<Invoice>().WithMany().HasForeignKey(item => new { item.TenantId, item.InvoiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<ClinicalService>().WithMany().HasForeignKey(item => new { item.TenantId, item.ServiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment", "billing", table =>
        {
            table.HasCheckConstraint("ck_billing_payment_status", "[status] BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_billing_payment_amounts",
                "[amount] > 0 AND [allocated_amount] >= 0 AND [allocated_amount] <= [amount]");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TenantId, item.Id });
        builder.HasAlternateKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.Ignore(item => item.UnallocatedAmount);
        builder.Property(item => item.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(item => item.ReceiptNumber).HasMaxLength(60).IsRequired();
        builder.Property(item => item.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(item => item.Amount).HasPrecision(19, 2);
        builder.Property(item => item.AllocatedAmount).HasPrecision(19, 2);
        builder.Property(item => item.Notes).HasMaxLength(1000);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.ReceiptNumber }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.PatientId, item.ConfirmedUtc });
        builder.HasOne<Branch>().WithMany().HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
        builder.HasOne<Patient>().WithMany().HasForeignKey(item => new { item.TenantId, item.PatientId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class PaymentTenderConfiguration : IEntityTypeConfiguration<PaymentTender>
{
    public void Configure(EntityTypeBuilder<PaymentTender> builder)
    {
        builder.ToTable("payment_tender", "billing", table =>
        {
            table.HasCheckConstraint("ck_billing_payment_tender_method", "[method] BETWEEN 1 AND 6");
            table.HasCheckConstraint("ck_billing_payment_tender_amount", "[amount] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Amount).HasPrecision(19, 2);
        builder.Property(item => item.ExternalReference).HasMaxLength(120);
        builder.Property(item => item.Narration).HasMaxLength(250);
        builder.HasIndex(item => new { item.TenantId, item.PaymentId });
        builder.HasOne<Payment>().WithMany().HasForeignKey(item => new { item.TenantId, item.PaymentId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("payment_allocation", "billing", table =>
            table.HasCheckConstraint("ck_billing_payment_allocation_amount", "[amount] > 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(item => item.Amount).HasPrecision(19, 2);
        builder.HasIndex(item => new { item.TenantId, item.RequestId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.PaymentId, item.InvoiceId });
        builder.HasOne<Payment>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.PaymentId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
        builder.HasOne<Invoice>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.BranchId, item.InvoiceId })
            .HasPrincipalKey(item => new { item.TenantId, item.BranchId, item.Id });
    }
}

internal sealed class FinancialDocumentSnapshotConfiguration : IEntityTypeConfiguration<FinancialDocumentSnapshot>
{
    public void Configure(EntityTypeBuilder<FinancialDocumentSnapshot> builder)
    {
        builder.ToTable("financial_document_snapshot", "billing", table =>
            table.HasCheckConstraint("ck_billing_document_kind", "[kind] BETWEEN 1 AND 2"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.DocumentNumber).HasMaxLength(60).IsRequired();
        builder.Property(item => item.SchemaVersion).HasMaxLength(40).IsRequired();
        builder.Property(item => item.PayloadJson).HasMaxLength(16000).IsRequired();
        builder.Property(item => item.PayloadHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Kind, item.SourceId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.BranchId, item.DocumentNumber }).IsUnique();
        builder.HasOne<Branch>().WithMany().HasForeignKey(item => new { item.TenantId, item.BranchId })
            .HasPrincipalKey(item => new { item.TenantId, item.Id });
    }
}
