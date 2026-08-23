using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Billing;

public sealed class Invoice : TenantScopedEntity
{
    private Invoice() { }

    public long BranchId { get; private set; }
    public long PatientId { get; private set; }
    public long? BookingId { get; private set; }
    public long? ContractId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "INR";
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal Total { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public decimal Balance => Total - AllocatedAmount;
    public InvoiceStatus Status { get; private set; }
    public string CalculationPolicyVersion { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTimeOffset IssuedUtc { get; private set; }
    public long IssuedByActorId { get; private set; }
    public long Version { get; private set; } = 1;

    public static Invoice Issue(long tenantId, long branchId, long patientId, long? bookingId, long? contractId,
        Guid requestId, string requestHash, string invoicePrefix, string currency, string calculationPolicyVersion, string? notes,
        long issuedByActorId, DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0 || patientId <= 0 || bookingId is <= 0 || contractId is <= 0
            || issuedByActorId <= 0 || requestId == Guid.Empty || string.IsNullOrWhiteSpace(requestHash))
            throw new DomainRuleException("Invoice scope, patient and issuing actor are required.");
        var invoice = new Invoice
        {
            TenantId = tenantId,
            BranchId = branchId,
            PatientId = patientId,
            BookingId = bookingId,
            ContractId = contractId,
            RequestId = requestId,
            RequestHash = requestHash,
            Currency = BillingMoney.Currency(currency),
            CalculationPolicyVersion = BillingMoney.Required(calculationPolicyVersion, 40, "Calculation policy version"),
            Notes = BillingMoney.Optional(notes, 1000),
            Status = InvoiceStatus.Issued,
            IssuedUtc = now,
            IssuedByActorId = issuedByActorId
        };
        invoice.InvoiceNumber = $"{BillingMoney.Required(invoicePrefix, 30, "Invoice prefix").ToUpperInvariant()}-{invoice.Id:X}";
        invoice.StampCreated(now);
        return invoice;
    }

    public void SetIssuedTotals(IReadOnlyCollection<InvoiceLine> lines)
    {
        if (lines.Count == 0 || lines.Any(item => item.InvoiceId != Id || item.TenantId != TenantId))
            throw new DomainRuleException("At least one valid line is required to issue an invoice.");
        if (Subtotal != 0 || Total != 0)
            throw new DomainRuleException("Issued invoice totals are immutable.");
        Subtotal = BillingMoney.Amount(lines.Sum(item => item.LineTotal), "Invoice subtotal");
        DiscountTotal = 0;
        TaxTotal = 0;
        Total = Subtotal;
    }

    public void Allocate(decimal amount, long expectedVersion, DateTimeOffset now)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The invoice changed after it was loaded.");
        var normalized = BillingMoney.Amount(amount, "Allocation amount");
        if (normalized > Balance)
            throw new DomainRuleException("Allocation cannot exceed the invoice balance.");
        AllocatedAmount += normalized;
        Status = AllocatedAmount == Total ? InvoiceStatus.Paid : InvoiceStatus.PartPaid;
        Version++;
        StampModified(now);
    }
}
