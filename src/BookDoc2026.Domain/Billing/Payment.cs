using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Billing;

public sealed class Payment : TenantScopedEntity
{
    private Payment() { }

    public long BranchId { get; private set; }
    public long PatientId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public string ReceiptNumber { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "INR";
    public decimal Amount { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public decimal UnallocatedAmount => Amount - AllocatedAmount;
    public PaymentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset ConfirmedUtc { get; private set; }
    public long ReceivedByActorId { get; private set; }
    public long Version { get; private set; } = 1;

    public static Payment Confirm(long tenantId, long branchId, long patientId, Guid requestId, string requestHash,
        string receiptPrefix, string currency, string? notes, long receivedByActorId, DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0 || patientId <= 0 || requestId == Guid.Empty
            || string.IsNullOrWhiteSpace(requestHash) || receivedByActorId <= 0)
            throw new DomainRuleException("Payment scope, patient, request evidence and receiver are required.");
        var payment = new Payment
        {
            TenantId = tenantId,
            BranchId = branchId,
            PatientId = patientId,
            RequestId = requestId,
            RequestHash = requestHash,
            Currency = BillingMoney.Currency(currency),
            Status = PaymentStatus.Confirmed,
            Notes = BillingMoney.Optional(notes, 1000),
            ConfirmedUtc = now,
            ReceivedByActorId = receivedByActorId
        };
        payment.ReceiptNumber = $"{BillingMoney.Required(receiptPrefix, 30, "Receipt prefix").ToUpperInvariant()}-{payment.Id:X}";
        payment.StampCreated(now);
        return payment;
    }

    public void SetConfirmedAmount(IReadOnlyCollection<PaymentTender> tenders)
    {
        if (tenders.Count == 0 || tenders.Any(item => item.PaymentId != Id || item.TenantId != TenantId))
            throw new DomainRuleException("At least one valid tender is required to confirm a payment.");
        if (Amount != 0) throw new DomainRuleException("Confirmed payment amount is immutable.");
        Amount = BillingMoney.Amount(tenders.Sum(item => item.Amount), "Payment amount");
    }

    public void Allocate(decimal amount, long expectedVersion, DateTimeOffset now)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The payment changed after it was loaded.");
        var normalized = BillingMoney.Amount(amount, "Allocation amount");
        if (normalized > UnallocatedAmount)
            throw new DomainRuleException("Allocation cannot exceed the unallocated payment amount.");
        AllocatedAmount += normalized;
        Status = AllocatedAmount == Amount ? PaymentStatus.FullyAllocated : PaymentStatus.Confirmed;
        Version++;
        StampModified(now);
    }
}
