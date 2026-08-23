namespace BookDoc2026.Contracts.Billing;

public sealed record IssueInvoiceLineRequest(
    string ServiceId,
    int Quantity,
    decimal UnitPrice);

public sealed record IssueInvoiceRequest(
    Guid RequestId,
    string PatientId,
    string? BookingId,
    string? ContractId,
    string Currency,
    string? Notes,
    IReadOnlyCollection<IssueInvoiceLineRequest> Lines);

public sealed record ReceivePaymentTenderRequest(
    string Method,
    decimal Amount,
    string? ExternalReference,
    string? Narration);

public sealed record ReceivePaymentRequest(
    Guid RequestId,
    string PatientId,
    string Currency,
    string? Notes,
    IReadOnlyCollection<ReceivePaymentTenderRequest> Tenders);

public sealed record AllocatePaymentRequest(
    Guid RequestId,
    string InvoiceId,
    decimal Amount,
    long ExpectedPaymentVersion,
    long ExpectedInvoiceVersion);

public sealed record FinancialDocumentSnapshotResponse(
    string Id,
    string Kind,
    string DocumentNumber,
    string SchemaVersion,
    string PayloadHash,
    DateTimeOffset CreatedUtc);

public sealed record InvoiceLineResponse(
    string Id,
    string ServiceId,
    string ServiceCode,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record InvoiceResponse(
    string Id,
    string PatientId,
    string? BookingId,
    string? ContractId,
    string InvoiceNumber,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal Total,
    decimal AllocatedAmount,
    decimal Balance,
    string Status,
    string CalculationPolicyVersion,
    DateTimeOffset IssuedUtc,
    string IssuedBySubjectId,
    long Version,
    bool IsReplay,
    IReadOnlyCollection<InvoiceLineResponse> Lines,
    FinancialDocumentSnapshotResponse Snapshot);

public sealed record PaymentTenderResponse(
    string Id,
    string Method,
    decimal Amount,
    string? ExternalReference,
    string? Narration);

public sealed record PaymentAllocationResponse(
    string Id,
    string InvoiceId,
    decimal Amount,
    DateTimeOffset AllocatedUtc,
    string AllocatedBySubjectId);

public sealed record PaymentResponse(
    string Id,
    string PatientId,
    string ReceiptNumber,
    string Currency,
    decimal Amount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    string Status,
    DateTimeOffset ConfirmedUtc,
    string ReceivedBySubjectId,
    long Version,
    bool IsReplay,
    IReadOnlyCollection<PaymentTenderResponse> Tenders,
    IReadOnlyCollection<PaymentAllocationResponse> Allocations,
    FinancialDocumentSnapshotResponse ReceiptSnapshot);
