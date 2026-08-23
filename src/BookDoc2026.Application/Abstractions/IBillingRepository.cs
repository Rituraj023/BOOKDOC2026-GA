using BookDoc2026.Domain.Billing;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Abstractions;

public sealed record BillingServiceReference(long Id, string Code, string Name);

public sealed record InvoiceAggregate(
    Invoice Invoice,
    IReadOnlyCollection<InvoiceLine> Lines,
    FinancialDocumentSnapshot Snapshot);

public sealed record PaymentAggregate(
    Payment Payment,
    IReadOnlyCollection<PaymentTender> Tenders,
    IReadOnlyCollection<PaymentAllocation> Allocations,
    FinancialDocumentSnapshot ReceiptSnapshot);

public interface IBillingRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<BranchConfiguration?> GetBranchConfigurationAsync(long branchId, CancellationToken cancellationToken);
    Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken);
    Task<bool> BookingMatchesAsync(long branchId, long bookingId, long patientId,
        CancellationToken cancellationToken);
    Task<bool> ContractMatchesAsync(long branchId, long contractId, long patientId,
        CancellationToken cancellationToken);
    Task<BillingServiceReference?> GetServiceAsync(long serviceId, CancellationToken cancellationToken);
    Task<InvoiceAggregate?> GetInvoiceByRequestAsync(Guid requestId, CancellationToken cancellationToken);
    Task<InvoiceAggregate?> GetInvoiceAsync(long branchId, long invoiceId, bool tracked,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InvoiceAggregate>> ListInvoicesAsync(long branchId, int take,
        CancellationToken cancellationToken);
    Task<PaymentAggregate?> GetPaymentByRequestAsync(Guid requestId, CancellationToken cancellationToken);
    Task<PaymentAggregate?> GetPaymentAsync(long branchId, long paymentId, bool tracked,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaymentAggregate>> ListPaymentsAsync(long branchId, int take,
        CancellationToken cancellationToken);
    Task<PaymentAllocation?> GetAllocationByRequestAsync(Guid requestId, CancellationToken cancellationToken);
    Task AddInvoiceAsync(Invoice invoice, IReadOnlyCollection<InvoiceLine> lines,
        FinancialDocumentSnapshot snapshot, AuditEvent audit, CancellationToken cancellationToken);
    Task AddPaymentAsync(Payment payment, IReadOnlyCollection<PaymentTender> tenders,
        FinancialDocumentSnapshot receiptSnapshot, AuditEvent audit, CancellationToken cancellationToken);
    Task AddAllocationAsync(PaymentAllocation allocation, AuditEvent audit, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
