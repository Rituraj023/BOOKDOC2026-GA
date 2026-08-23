using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Billing;
using BookDoc2026.Contracts.Billing;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/billing")]
public sealed class BillingController(BillingService service, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.BillingInvoicesView)]
    [HttpGet("invoices")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<InvoiceResponse>>>> ListInvoices(
        string branchId, [FromQuery] int take, CancellationToken cancellationToken) => Ok(
        new ApiEnvelope<IReadOnlyCollection<InvoiceResponse>>(
            await service.ListInvoicesAsync(ids.Tenant(PublicIdKind.Branch, branchId), take, cancellationToken),
            HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.BillingInvoicesIssue)]
    [HttpPost("invoices")]
    public async Task<ActionResult<ApiEnvelope<InvoiceResponse>>> IssueInvoice(string branchId,
        IssueInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await service.IssueInvoiceAsync(ids.Tenant(PublicIdKind.Branch, branchId), request,
            cancellationToken);
        return Created($"/api/v1/branches/{branchId}/billing/invoices/{response.Id}",
            new ApiEnvelope<InvoiceResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.BillingInvoicesView)]
    [HttpGet("invoices/{invoiceId}")]
    public async Task<ActionResult<ApiEnvelope<InvoiceResponse>>> GetInvoice(string branchId, string invoiceId,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<InvoiceResponse>(
            await service.GetInvoiceAsync(ids.Tenant(PublicIdKind.Branch, branchId),
                ids.Tenant(PublicIdKind.Invoice, invoiceId), cancellationToken), HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.BillingPaymentsReceive)]
    [HttpPost("payments")]
    public async Task<ActionResult<ApiEnvelope<PaymentResponse>>> ReceivePayment(string branchId,
        ReceivePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await service.ReceivePaymentAsync(ids.Tenant(PublicIdKind.Branch, branchId), request,
            cancellationToken);
        return Created($"/api/v1/branches/{branchId}/billing/payments/{response.Id}",
            new ApiEnvelope<PaymentResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.BillingPaymentsView)]
    [HttpGet("payments")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<PaymentResponse>>>> ListPayments(
        string branchId, [FromQuery] int take, CancellationToken cancellationToken) => Ok(
        new ApiEnvelope<IReadOnlyCollection<PaymentResponse>>(
            await service.ListPaymentsAsync(ids.Tenant(PublicIdKind.Branch, branchId), take, cancellationToken),
            HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.BillingPaymentsView)]
    [HttpGet("payments/{paymentId}")]
    public async Task<ActionResult<ApiEnvelope<PaymentResponse>>> GetPayment(string branchId, string paymentId,
        CancellationToken cancellationToken) => Ok(new ApiEnvelope<PaymentResponse>(
            await service.GetPaymentAsync(ids.Tenant(PublicIdKind.Branch, branchId),
                ids.Tenant(PublicIdKind.Payment, paymentId), cancellationToken), HttpContext.TraceIdentifier));

    [Authorize(Policy = FoundationPermissions.BillingPaymentsAllocate)]
    [HttpPost("payments/{paymentId}/allocations")]
    public async Task<ActionResult<ApiEnvelope<PaymentResponse>>> Allocate(string branchId, string paymentId,
        AllocatePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await service.AllocateAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Payment, paymentId), request, cancellationToken);
        return Created($"/api/v1/branches/{branchId}/billing/payments/{paymentId}",
            new ApiEnvelope<PaymentResponse>(response, HttpContext.TraceIdentifier));
    }
}
