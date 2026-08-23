using BookDoc2026.Contracts.Billing;

namespace BookDoc2026.Blazor.UI.Billing;

public sealed record CashierInvoiceLineDraft(
    string ServiceId,
    string ServiceCode,
    string Description,
    int Quantity,
    decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

public sealed record CashierTenderDraft(
    string Method,
    decimal Amount,
    string? ExternalReference,
    string? Narration);

public sealed class CashierBillingDraft
{
    private readonly List<CashierInvoiceLineDraft> invoiceLines = [];
    private readonly List<CashierTenderDraft> tenders = [];

    public Guid InvoiceRequestId { get; private set; } = Guid.NewGuid();
    public Guid PaymentRequestId { get; private set; } = Guid.NewGuid();
    public Guid AllocationRequestId { get; private set; } = Guid.NewGuid();
    public IReadOnlyList<CashierInvoiceLineDraft> InvoiceLines => invoiceLines;
    public IReadOnlyList<CashierTenderDraft> Tenders => tenders;
    public InvoiceResponse? Invoice { get; private set; }
    public PaymentResponse? Payment { get; private set; }
    public decimal InvoiceDraftTotal => invoiceLines.Sum(item => item.LineTotal);
    public decimal TenderTotal => tenders.Sum(item => item.Amount);

    public void AddInvoiceLine(string serviceId, string serviceCode, string description, int quantity,
        decimal unitPrice)
    {
        if (Invoice is not null) throw new InvalidOperationException("The invoice has already been issued.");
        if (string.IsNullOrWhiteSpace(serviceId) || string.IsNullOrWhiteSpace(serviceCode)
            || string.IsNullOrWhiteSpace(description) || quantity is < 1 or > 1000 || unitPrice < 0)
            throw new ArgumentException("A valid service, quantity and non-negative unit price are required.");
        invoiceLines.Add(new(serviceId.Trim(), serviceCode.Trim(), description.Trim(), quantity, unitPrice));
    }

    public void RemoveInvoiceLine(int index)
    {
        if (Invoice is not null) throw new InvalidOperationException("Issued invoice lines are immutable.");
        invoiceLines.RemoveAt(index);
    }

    public IssueInvoiceRequest BuildInvoiceRequest(string patientId, string? bookingId, string? contractId,
        string currency, string? notes)
    {
        if (invoiceLines.Count == 0 || InvoiceDraftTotal <= 0)
            throw new InvalidOperationException("Add at least one positively priced invoice line.");
        return new(InvoiceRequestId, Required(patientId, "patient"), Clean(bookingId), Clean(contractId),
            Required(currency, "currency"), Clean(notes), invoiceLines.Select(item =>
                new IssueInvoiceLineRequest(item.ServiceId, item.Quantity, item.UnitPrice)).ToArray());
    }

    public void CompleteInvoice(InvoiceResponse invoice) => Invoice = invoice
        ?? throw new ArgumentNullException(nameof(invoice));

    public void AddTender(string method, decimal amount, string? externalReference, string? narration)
    {
        if (Payment is not null) throw new InvalidOperationException("The payment has already been confirmed.");
        if (string.IsNullOrWhiteSpace(method) || amount <= 0)
            throw new ArgumentException("A payment method and positive amount are required.");
        tenders.Add(new(method.Trim(), amount, Clean(externalReference), Clean(narration)));
    }

    public void RemoveTender(int index)
    {
        if (Payment is not null) throw new InvalidOperationException("Confirmed payment tenders are immutable.");
        tenders.RemoveAt(index);
    }

    public ReceivePaymentRequest BuildPaymentRequest(string patientId, string currency, string? notes)
    {
        if (tenders.Count == 0) throw new InvalidOperationException("Add at least one payment tender.");
        return new(PaymentRequestId, Required(patientId, "patient"), Required(currency, "currency"), Clean(notes),
            tenders.Select(item => new ReceivePaymentTenderRequest(item.Method, item.Amount,
                item.ExternalReference, item.Narration)).ToArray());
    }

    public void CompletePayment(PaymentResponse payment) => Payment = payment
        ?? throw new ArgumentNullException(nameof(payment));

    public AllocatePaymentRequest BuildAllocationRequest(decimal amount)
    {
        if (Invoice is null || Payment is null)
            throw new InvalidOperationException("Issue an invoice and confirm a payment before allocation.");
        if (amount <= 0 || amount > Invoice.Balance || amount > Payment.UnallocatedAmount)
            throw new InvalidOperationException("Allocation must fit both the invoice balance and payment remainder.");
        return new(AllocationRequestId, Invoice.Id, amount, Payment.Version, Invoice.Version);
    }

    public void CompleteAllocation(PaymentResponse payment, InvoiceResponse invoice)
    {
        Payment = payment ?? throw new ArgumentNullException(nameof(payment));
        Invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
        AllocationRequestId = Guid.NewGuid();
    }

    public void BeginAdditionalPayment()
    {
        if (Invoice is null || Payment is null || Invoice.Balance <= 0 || Payment.UnallocatedAmount != 0)
            throw new InvalidOperationException(
                "Another payment can begin only when the current payment is fully allocated and the invoice remains open.");
        tenders.Clear();
        Payment = null;
        PaymentRequestId = Guid.NewGuid();
        AllocationRequestId = Guid.NewGuid();
    }

    public void Reset()
    {
        invoiceLines.Clear();
        tenders.Clear();
        Invoice = null;
        Payment = null;
        InvoiceRequestId = Guid.NewGuid();
        PaymentRequestId = Guid.NewGuid();
        AllocationRequestId = Guid.NewGuid();
    }

    private static string Required(string value, string label) => string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException($"A {label} is required.") : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
