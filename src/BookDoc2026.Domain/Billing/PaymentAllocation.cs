using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Billing;

public sealed class PaymentAllocation : TenantScopedEntity
{
    private PaymentAllocation() { }

    public long BranchId { get; private set; }
    public long PaymentId { get; private set; }
    public long InvoiceId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset AllocatedUtc { get; private set; }
    public long AllocatedByActorId { get; private set; }

    public static PaymentAllocation Create(Payment payment, Invoice invoice, Guid requestId, string requestHash,
        decimal amount, long expectedPaymentVersion, long expectedInvoiceVersion, long actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(invoice);
        if (payment.TenantId != invoice.TenantId || payment.BranchId != invoice.BranchId
            || payment.PatientId != invoice.PatientId || !string.Equals(payment.Currency, invoice.Currency,
                StringComparison.Ordinal) || requestId == Guid.Empty || string.IsNullOrWhiteSpace(requestHash)
            || actorId <= 0)
            throw new DomainRuleException("Payment and invoice allocation scope, patient, currency or request evidence do not match.");
        var normalized = BillingMoney.Amount(amount, "Allocation amount");

        // Validate both sides before either aggregate is changed. This keeps the in-memory
        // operation atomic even when the second balance or version check would fail.
        if (payment.Version != expectedPaymentVersion)
            throw new ConcurrencyConflictException("The payment changed after it was loaded.");
        if (invoice.Version != expectedInvoiceVersion)
            throw new ConcurrencyConflictException("The invoice changed after it was loaded.");
        if (normalized > payment.UnallocatedAmount)
            throw new DomainRuleException("Allocation cannot exceed the unallocated payment amount.");
        if (normalized > invoice.Balance)
            throw new DomainRuleException("Allocation cannot exceed the invoice balance.");

        payment.Allocate(normalized, expectedPaymentVersion, now);
        invoice.Allocate(normalized, expectedInvoiceVersion, now);
        var allocation = new PaymentAllocation
        {
            TenantId = payment.TenantId,
            BranchId = payment.BranchId,
            PaymentId = payment.Id,
            InvoiceId = invoice.Id,
            RequestId = requestId,
            RequestHash = requestHash,
            Amount = normalized,
            AllocatedUtc = now,
            AllocatedByActorId = actorId
        };
        allocation.StampCreated(now);
        return allocation;
    }
}
