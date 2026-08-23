using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Billing;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Billing;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Billing;

public sealed class BillingService(
    IBillingRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlation)
{
    private const string CalculationPolicy = "BASIC-NET-V1";
    private const string SnapshotSchema = "FINANCIAL-DOCUMENT-V1";

    public async Task<InvoiceResponse> IssueInvoiceAsync(long branchId, IssueInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.BillingInvoicesIssue, branchId, cancellationToken);
        if (request.RequestId == Guid.Empty) throw new DomainRuleException("Invoice request identifier is required.");
        var patientId = publicIds.Decode(PublicIdKind.Patient, request.PatientId, branch.TenantId);
        var bookingId = publicIds.DecodeOptional(PublicIdKind.Booking, request.BookingId, branch.TenantId);
        var contractId = publicIds.DecodeOptional(PublicIdKind.Contract, request.ContractId, branch.TenantId);
        var decodedLines = request.Lines.Select(item => new
        {
            Request = item,
            ServiceId = publicIds.Decode(PublicIdKind.ClinicalService, item.ServiceId, branch.TenantId)
        }).ToArray();
        var requestHash = Hash(new
        {
            patientId, bookingId, contractId, Currency = request.Currency.Trim().ToUpperInvariant(),
            Notes = request.Notes?.Trim(), Lines = decodedLines.Select(item => new
            { item.ServiceId, item.Request.Quantity, item.Request.UnitPrice }).ToArray()
        });
        var replay = await repository.GetInvoiceByRequestAsync(request.RequestId, cancellationToken);
        if (replay is not null)
        {
            if (!string.Equals(replay.Invoice.RequestHash, requestHash, StringComparison.Ordinal))
                throw new DomainRuleException("Invoice request identifier was reused with different content.");
            return Map(replay, true);
        }
        if (!await repository.PatientExistsAsync(patientId, cancellationToken))
            throw new NotFoundException("Patient was not found in the current tenant.");
        if (bookingId.HasValue && !await repository.BookingMatchesAsync(branchId, bookingId.Value, patientId,
                cancellationToken))
            throw new DomainRuleException("Only a confirmed Booking for this branch and patient can be invoiced.");
        if (contractId.HasValue && !await repository.ContractMatchesAsync(branchId, contractId.Value, patientId,
                cancellationToken))
            throw new DomainRuleException("Contract does not belong to this branch and patient.");
        if (decodedLines.Length == 0) throw new DomainRuleException("At least one invoice line is required.");
        var configuration = await repository.GetBranchConfigurationAsync(branchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch configuration was not provisioned.");
        var now = clock.UtcNow;
        var invoice = Invoice.Issue(branch.TenantId, branchId, patientId, bookingId, contractId,
            request.RequestId, requestHash, configuration.InvoicePrefix, request.Currency, CalculationPolicy,
            request.Notes, actor.ActorId, now);
        var lines = new List<InvoiceLine>(decodedLines.Length);
        foreach (var item in decodedLines)
        {
            var service = await repository.GetServiceAsync(item.ServiceId, cancellationToken)
                ?? throw new NotFoundException("An invoice service was not found or is inactive.");
            lines.Add(InvoiceLine.Create(invoice, service.Id, service.Code, service.Name,
                item.Request.Quantity, item.Request.UnitPrice, now));
        }
        invoice.SetIssuedTotals(lines);
        var snapshot = FinancialDocumentSnapshot.Capture(branch.TenantId, branchId, FinancialDocumentKind.Invoice,
            invoice.Id, invoice.InvoiceNumber, SnapshotSchema, InvoicePayload(invoice, lines), now);
        var audit = Audit(branch, invoice.Id, "Invoice.Issued", new
        {
            invoice.PatientId, invoice.BookingId, invoice.ContractId, invoice.InvoiceNumber,
            invoice.Currency, invoice.Total, LineCount = lines.Count, snapshot.PayloadHash
        }, now);
        await repository.AddInvoiceAsync(invoice, lines, snapshot, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new InvoiceAggregate(invoice, lines, snapshot), false);
    }

    public async Task<InvoiceResponse> GetInvoiceAsync(long branchId, long invoiceId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.BillingInvoicesView, branchId, cancellationToken);
        return Map(await repository.GetInvoiceAsync(branchId, invoiceId, false, cancellationToken)
            ?? throw new NotFoundException("Invoice was not found."), false);
    }

    public async Task<IReadOnlyCollection<InvoiceResponse>> ListInvoicesAsync(long branchId, int take,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.BillingInvoicesView, branchId, cancellationToken);
        var limited = Math.Clamp(take, 1, 100);
        return (await repository.ListInvoicesAsync(branchId, limited, cancellationToken))
            .Select(item => Map(item, false)).ToArray();
    }

    public async Task<PaymentResponse> ReceivePaymentAsync(long branchId, ReceivePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.BillingPaymentsReceive, branchId,
            cancellationToken);
        if (request.RequestId == Guid.Empty) throw new DomainRuleException("Payment request identifier is required.");
        var patientId = publicIds.Decode(PublicIdKind.Patient, request.PatientId, branch.TenantId);
        var decodedTenders = request.Tenders.Select(item => new
        {
            Request = item,
            Method = Parse<PaymentMethod>(item.Method, "Payment method")
        }).ToArray();
        var requestHash = Hash(new
        {
            patientId, Currency = request.Currency.Trim().ToUpperInvariant(), Notes = request.Notes?.Trim(),
            Tenders = decodedTenders.Select(item => new { item.Method, item.Request.Amount,
                ExternalReference = item.Request.ExternalReference?.Trim(), Narration = item.Request.Narration?.Trim() })
        });
        var replay = await repository.GetPaymentByRequestAsync(request.RequestId, cancellationToken);
        if (replay is not null)
        {
            if (!string.Equals(replay.Payment.RequestHash, requestHash, StringComparison.Ordinal))
                throw new DomainRuleException("Payment request identifier was reused with different content.");
            return Map(replay, true);
        }
        if (!await repository.PatientExistsAsync(patientId, cancellationToken))
            throw new NotFoundException("Patient was not found in the current tenant.");
        if (decodedTenders.Length == 0) throw new DomainRuleException("At least one payment tender is required.");
        var configuration = await repository.GetBranchConfigurationAsync(branchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch configuration was not provisioned.");
        var now = clock.UtcNow;
        var payment = Payment.Confirm(branch.TenantId, branchId, patientId, request.RequestId, requestHash,
            configuration.ReceiptPrefix, request.Currency, request.Notes, actor.ActorId, now);
        var tenders = decodedTenders.Select(item => PaymentTender.Create(payment, item.Method, item.Request.Amount,
            item.Request.ExternalReference, item.Request.Narration, now)).ToArray();
        payment.SetConfirmedAmount(tenders);
        var snapshot = FinancialDocumentSnapshot.Capture(branch.TenantId, branchId, FinancialDocumentKind.Receipt,
            payment.Id, payment.ReceiptNumber, SnapshotSchema, ReceiptPayload(payment, tenders), now);
        var audit = Audit(branch, payment.Id, "Payment.Received", new
        {
            payment.PatientId, payment.ReceiptNumber, payment.Currency, payment.Amount,
            TenderCount = tenders.Length, snapshot.PayloadHash
        }, now);
        await repository.AddPaymentAsync(payment, tenders, snapshot, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new PaymentAggregate(payment, tenders, [], snapshot), false);
    }

    public async Task<PaymentResponse> GetPaymentAsync(long branchId, long paymentId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.BillingPaymentsView, branchId, cancellationToken);
        return Map(await repository.GetPaymentAsync(branchId, paymentId, false, cancellationToken)
            ?? throw new NotFoundException("Payment was not found."), false);
    }

    public async Task<IReadOnlyCollection<PaymentResponse>> ListPaymentsAsync(long branchId, int take,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.BillingPaymentsView, branchId, cancellationToken);
        var limited = Math.Clamp(take, 1, 100);
        return (await repository.ListPaymentsAsync(branchId, limited, cancellationToken))
            .Select(item => Map(item, false)).ToArray();
    }

    public async Task<PaymentResponse> AllocateAsync(long branchId, long paymentId, AllocatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.BillingPaymentsAllocate, branchId,
            cancellationToken);
        if (request.RequestId == Guid.Empty) throw new DomainRuleException("Allocation request identifier is required.");
        var invoiceId = publicIds.Decode(PublicIdKind.Invoice, request.InvoiceId, branch.TenantId);
        var requestHash = Hash(new { paymentId, invoiceId, request.Amount });
        var replayAllocation = await repository.GetAllocationByRequestAsync(request.RequestId, cancellationToken);
        if (replayAllocation is not null)
        {
            if (!string.Equals(replayAllocation.RequestHash, requestHash, StringComparison.Ordinal)
                || replayAllocation.PaymentId != paymentId)
                throw new DomainRuleException("Allocation request identifier was reused with different content.");
            return Map(await repository.GetPaymentAsync(branchId, paymentId, false, cancellationToken)
                ?? throw new NotFoundException("Payment was not found."), true);
        }
        var payment = await repository.GetPaymentAsync(branchId, paymentId, true, cancellationToken)
            ?? throw new NotFoundException("Payment was not found.");
        var invoice = await repository.GetInvoiceAsync(branchId, invoiceId, true, cancellationToken)
            ?? throw new NotFoundException("Invoice was not found.");
        var now = clock.UtcNow;
        var allocation = PaymentAllocation.Create(payment.Payment, invoice.Invoice, request.RequestId, requestHash,
            request.Amount, request.ExpectedPaymentVersion, request.ExpectedInvoiceVersion, actor.ActorId, now);
        var audit = Audit(branch, allocation.Id, "Payment.Allocated", new
        {
            allocation.PaymentId, allocation.InvoiceId, allocation.Amount,
            PaymentVersion = payment.Payment.Version, InvoiceVersion = invoice.Invoice.Version
        }, now);
        await repository.AddAllocationAsync(allocation, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(payment with { Allocations = payment.Allocations.Append(allocation).ToArray() }, false);
    }

    private async Task<Branch> RequireBranchAsync(string permission, long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this billing operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private AuditEvent Audit(Branch branch, long entityId, string action, object metadata, DateTimeOffset now) =>
        AuditEvent.Record(branch.TenantId, branch.Id, actor.ActorId, action, "Billing", entityId,
            JsonSerializer.Serialize(metadata), correlation.CorrelationId, now);

    private static string InvoicePayload(Invoice invoice, IReadOnlyCollection<InvoiceLine> lines) =>
        JsonSerializer.Serialize(new
        {
            Schema = SnapshotSchema, Kind = "Invoice", invoice.InvoiceNumber, invoice.PatientId,
            invoice.BookingId, invoice.ContractId, invoice.Currency, invoice.Subtotal, invoice.DiscountTotal,
            invoice.TaxTotal, invoice.Total, invoice.CalculationPolicyVersion, invoice.IssuedUtc,
            Lines = lines.Select(item => new { item.ServiceId, item.ServiceCodeSnapshot,
                item.DescriptionSnapshot, item.Quantity, item.UnitPrice, item.LineTotal }).ToArray()
        });

    private static string ReceiptPayload(Payment payment, IReadOnlyCollection<PaymentTender> tenders) =>
        JsonSerializer.Serialize(new
        {
            Schema = SnapshotSchema, Kind = "Receipt", payment.ReceiptNumber, payment.PatientId,
            payment.Currency, payment.Amount, payment.ConfirmedUtc,
            Tenders = tenders.Select(item => new { Method = item.Method.ToString(), item.Amount,
                item.ExternalReference, item.Narration }).ToArray()
        });

    private static string Hash(object value) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));

    private static T Parse<T>(string value, string label) where T : struct, Enum =>
        Enum.TryParse<T>(value?.Trim(), true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed : throw new DomainRuleException($"{label} is not supported.");

    private InvoiceResponse Map(InvoiceAggregate aggregate, bool replay) => new(
        publicIds.Encode(PublicIdKind.Invoice, aggregate.Invoice.Id, aggregate.Invoice.TenantId),
        publicIds.Encode(PublicIdKind.Patient, aggregate.Invoice.PatientId, aggregate.Invoice.TenantId),
        publicIds.EncodeOptional(PublicIdKind.Booking, aggregate.Invoice.BookingId, aggregate.Invoice.TenantId),
        publicIds.EncodeOptional(PublicIdKind.Contract, aggregate.Invoice.ContractId, aggregate.Invoice.TenantId),
        aggregate.Invoice.InvoiceNumber, aggregate.Invoice.Currency, aggregate.Invoice.Subtotal,
        aggregate.Invoice.DiscountTotal, aggregate.Invoice.TaxTotal, aggregate.Invoice.Total,
        aggregate.Invoice.AllocatedAmount, aggregate.Invoice.Balance, aggregate.Invoice.Status.ToString(),
        aggregate.Invoice.CalculationPolicyVersion, aggregate.Invoice.IssuedUtc,
        publicIds.Encode(PublicIdKind.IdentitySubject, aggregate.Invoice.IssuedByActorId), aggregate.Invoice.Version,
        replay, aggregate.Lines.Select(item => new InvoiceLineResponse(
            publicIds.Encode(PublicIdKind.InvoiceLine, item.Id, item.TenantId),
            publicIds.Encode(PublicIdKind.ClinicalService, item.ServiceId, item.TenantId), item.ServiceCodeSnapshot,
            item.DescriptionSnapshot, item.Quantity, item.UnitPrice, item.LineTotal)).ToArray(),
        Map(aggregate.Snapshot));

    private PaymentResponse Map(PaymentAggregate aggregate, bool replay) => new(
        publicIds.Encode(PublicIdKind.Payment, aggregate.Payment.Id, aggregate.Payment.TenantId),
        publicIds.Encode(PublicIdKind.Patient, aggregate.Payment.PatientId, aggregate.Payment.TenantId),
        aggregate.Payment.ReceiptNumber, aggregate.Payment.Currency, aggregate.Payment.Amount,
        aggregate.Payment.AllocatedAmount, aggregate.Payment.UnallocatedAmount, aggregate.Payment.Status.ToString(),
        aggregate.Payment.ConfirmedUtc,
        publicIds.Encode(PublicIdKind.IdentitySubject, aggregate.Payment.ReceivedByActorId), aggregate.Payment.Version,
        replay, aggregate.Tenders.Select(item => new PaymentTenderResponse(
            publicIds.Encode(PublicIdKind.PaymentTender, item.Id, item.TenantId), item.Method.ToString(), item.Amount,
            item.ExternalReference, item.Narration)).ToArray(),
        aggregate.Allocations.OrderBy(item => item.AllocatedUtc).Select(item => new PaymentAllocationResponse(
            publicIds.Encode(PublicIdKind.PaymentAllocation, item.Id, item.TenantId),
            publicIds.Encode(PublicIdKind.Invoice, item.InvoiceId, item.TenantId), item.Amount, item.AllocatedUtc,
            publicIds.Encode(PublicIdKind.IdentitySubject, item.AllocatedByActorId))).ToArray(),
        Map(aggregate.ReceiptSnapshot));

    private FinancialDocumentSnapshotResponse Map(FinancialDocumentSnapshot snapshot) => new(
        publicIds.Encode(PublicIdKind.FinancialDocumentSnapshot, snapshot.Id, snapshot.TenantId),
        snapshot.Kind.ToString(), snapshot.DocumentNumber, snapshot.SchemaVersion, snapshot.PayloadHash,
        snapshot.CreatedUtc);
}
