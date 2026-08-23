using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Billing;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Contracts;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class BillingRepository(BookDocDbContext db) : IBillingRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        db.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public Task<BranchConfiguration?> GetBranchConfigurationAsync(long branchId,
        CancellationToken cancellationToken) => db.BranchConfigurations.AsNoTracking()
        .SingleOrDefaultAsync(item => item.BranchId == branchId, cancellationToken);

    public Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken) =>
        db.Patients.AnyAsync(item => item.Id == patientId && item.Status == PatientStatus.Active, cancellationToken);

    public Task<bool> BookingMatchesAsync(long branchId, long bookingId, long patientId,
        CancellationToken cancellationToken) => db.Bookings.AnyAsync(item => item.Id == bookingId
            && item.BranchId == branchId && item.PatientId == patientId && item.Status == BookingStatus.Confirmed,
        cancellationToken);

    public Task<bool> ContractMatchesAsync(long branchId, long contractId, long patientId,
        CancellationToken cancellationToken) => db.Contracts.AnyAsync(item => item.Id == contractId
            && item.BranchId == branchId && item.PatientId == patientId, cancellationToken);

    public async Task<BillingServiceReference?> GetServiceAsync(long serviceId,
        CancellationToken cancellationToken)
    {
        var service = await db.ClinicalServices.AsNoTracking().SingleOrDefaultAsync(item => item.Id == serviceId
            && item.Status == CatalogItemStatus.Active, cancellationToken);
        return service is null ? null : new(service.Id, service.Code, service.Name);
    }

    public async Task<InvoiceAggregate?> GetInvoiceByRequestAsync(Guid requestId,
        CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.AsNoTracking().SingleOrDefaultAsync(item => item.RequestId == requestId,
            cancellationToken);
        return invoice is null ? null : await InvoiceAggregateAsync(invoice, false, cancellationToken);
    }

    public async Task<InvoiceAggregate?> GetInvoiceAsync(long branchId, long invoiceId, bool tracked,
        CancellationToken cancellationToken)
    {
        var query = db.Invoices.Where(item => item.BranchId == branchId && item.Id == invoiceId);
        var invoice = await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        return invoice is null ? null : await InvoiceAggregateAsync(invoice, tracked, cancellationToken);
    }

    public async Task<IReadOnlyCollection<InvoiceAggregate>> ListInvoicesAsync(long branchId, int take,
        CancellationToken cancellationToken)
    {
        var invoices = await db.Invoices.AsNoTracking().Where(item => item.BranchId == branchId)
            .OrderByDescending(item => item.IssuedUtc).ThenByDescending(item => item.Id)
            .Take(take).ToArrayAsync(cancellationToken);
        var result = new List<InvoiceAggregate>(invoices.Length);
        foreach (var invoice in invoices)
            result.Add(await InvoiceAggregateAsync(invoice, false, cancellationToken));
        return result;
    }

    public async Task<PaymentAggregate?> GetPaymentByRequestAsync(Guid requestId,
        CancellationToken cancellationToken)
    {
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(item => item.RequestId == requestId,
            cancellationToken);
        return payment is null ? null : await PaymentAggregateAsync(payment, false, cancellationToken);
    }

    public async Task<PaymentAggregate?> GetPaymentAsync(long branchId, long paymentId, bool tracked,
        CancellationToken cancellationToken)
    {
        var query = db.Payments.Where(item => item.BranchId == branchId && item.Id == paymentId);
        var payment = await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        return payment is null ? null : await PaymentAggregateAsync(payment, tracked, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentAggregate>> ListPaymentsAsync(long branchId, int take,
        CancellationToken cancellationToken)
    {
        var payments = await db.Payments.AsNoTracking().Where(item => item.BranchId == branchId)
            .OrderByDescending(item => item.ConfirmedUtc).ThenByDescending(item => item.Id)
            .Take(take).ToArrayAsync(cancellationToken);
        var result = new List<PaymentAggregate>(payments.Length);
        foreach (var payment in payments)
            result.Add(await PaymentAggregateAsync(payment, false, cancellationToken));
        return result;
    }

    public Task<PaymentAllocation?> GetAllocationByRequestAsync(Guid requestId,
        CancellationToken cancellationToken) => db.PaymentAllocations.AsNoTracking()
        .SingleOrDefaultAsync(item => item.RequestId == requestId, cancellationToken);

    public async Task AddInvoiceAsync(Invoice invoice, IReadOnlyCollection<InvoiceLine> lines,
        FinancialDocumentSnapshot snapshot, AuditEvent audit, CancellationToken cancellationToken)
    {
        await db.Invoices.AddAsync(invoice, cancellationToken);
        await db.InvoiceLines.AddRangeAsync(lines, cancellationToken);
        await db.FinancialDocumentSnapshots.AddAsync(snapshot, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public async Task AddPaymentAsync(Payment payment, IReadOnlyCollection<PaymentTender> tenders,
        FinancialDocumentSnapshot receiptSnapshot, AuditEvent audit, CancellationToken cancellationToken)
    {
        await db.Payments.AddAsync(payment, cancellationToken);
        await db.PaymentTenders.AddRangeAsync(tenders, cancellationToken);
        await db.FinancialDocumentSnapshots.AddAsync(receiptSnapshot, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public async Task AddAllocationAsync(PaymentAllocation allocation, AuditEvent audit,
        CancellationToken cancellationToken)
    {
        await db.PaymentAllocations.AddAsync(allocation, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    private async Task<InvoiceAggregate> InvoiceAggregateAsync(Invoice invoice, bool tracked,
        CancellationToken cancellationToken)
    {
        var lines = db.InvoiceLines.Where(item => item.InvoiceId == invoice.Id);
        var snapshots = db.FinancialDocumentSnapshots.Where(item => item.Kind == FinancialDocumentKind.Invoice
            && item.SourceId == invoice.Id);
        return new(invoice, await (tracked ? lines : lines.AsNoTracking()).ToArrayAsync(cancellationToken),
            await (tracked ? snapshots : snapshots.AsNoTracking()).SingleAsync(cancellationToken));
    }

    private async Task<PaymentAggregate> PaymentAggregateAsync(Payment payment, bool tracked,
        CancellationToken cancellationToken)
    {
        var tenders = db.PaymentTenders.Where(item => item.PaymentId == payment.Id);
        var allocations = db.PaymentAllocations.Where(item => item.PaymentId == payment.Id);
        var snapshots = db.FinancialDocumentSnapshots.Where(item => item.Kind == FinancialDocumentKind.Receipt
            && item.SourceId == payment.Id);
        return new(payment, await (tracked ? tenders : tenders.AsNoTracking()).ToArrayAsync(cancellationToken),
            await (tracked ? allocations : allocations.AsNoTracking()).ToArrayAsync(cancellationToken),
            await (tracked ? snapshots : snapshots.AsNoTracking()).SingleAsync(cancellationToken));
    }
}
