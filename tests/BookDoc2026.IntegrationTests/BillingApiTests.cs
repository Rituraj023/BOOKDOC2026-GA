using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Contracts.Billing;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class BillingApiTests
{
    [Fact]
    public async Task BillingFlow_IsIdempotentAllocatedImmutableAndTenantProtected()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client, "Billing Clinic");
        var (patient, service) = await CreatePatientAndServiceAsync(client, tenant);
        var invoiceRequestId = Guid.NewGuid();
        var invoiceRequest = new IssueInvoiceRequest(invoiceRequestId, patient.Id, null, null, "INR", null,
            [new IssueInvoiceLineRequest(service.Id, 2, 125m)]);

        SetHeaders(client, tenant, FoundationPermissions.BillingInvoicesView);
        var forbiddenIssue = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/billing/invoices", invoiceRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenIssue.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.BillingInvoicesIssue);
        var invoice = await PostCreated<InvoiceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/billing/invoices", invoiceRequest);
        Assert.Equal(250m, invoice.Total);
        Assert.Equal(250m, invoice.Balance);
        Assert.Equal("Issued", invoice.Status);
        Assert.False(invoice.IsReplay);
        Assert.DoesNotMatch("^[0-9]+$", invoice.Id);
        Assert.Equal(64, invoice.Snapshot.PayloadHash.Length);

        var forbiddenInvoiceList = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/billing/invoices?take=500");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenInvoiceList.StatusCode);
        SetHeaders(client, tenant, FoundationPermissions.BillingInvoicesView);
        var invoiceList = await client.GetFromJsonAsync<ApiEnvelope<IReadOnlyCollection<InvoiceResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/billing/invoices?take=500");
        Assert.Single(invoiceList!.Data);
        Assert.Equal(invoice.InvoiceNumber, invoiceList.Data.Single().InvoiceNumber);
        SetHeaders(client, tenant, FoundationPermissions.BillingInvoicesIssue);

        var invoiceReplay = await PostCreated<InvoiceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/billing/invoices", invoiceRequest);
        Assert.True(invoiceReplay.IsReplay);
        Assert.Equal(invoice.InvoiceNumber, invoiceReplay.InvoiceNumber);
        var changedInvoiceReplay = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/billing/invoices",
            invoiceRequest with { Lines = [new IssueInvoiceLineRequest(service.Id, 2, 126m)] });
        Assert.Equal(HttpStatusCode.BadRequest, changedInvoiceReplay.StatusCode);

        var paymentRequest = new ReceivePaymentRequest(Guid.NewGuid(), patient.Id, "INR", null,
            [new ReceivePaymentTenderRequest("Cash", 100m, null, null),
                new ReceivePaymentTenderRequest("Upi", 200m, "TEST-UPI-REFERENCE", null)]);
        SetHeaders(client, tenant, FoundationPermissions.BillingPaymentsReceive);
        var payment = await PostCreated<PaymentResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/billing/payments", paymentRequest);
        Assert.Equal(300m, payment.Amount);
        Assert.Equal(300m, payment.UnallocatedAmount);
        Assert.Equal(2, payment.Tenders.Count);
        var forbiddenPaymentList = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/billing/payments?take=50");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPaymentList.StatusCode);
        SetHeaders(client, tenant, FoundationPermissions.BillingPaymentsView);
        var paymentList = await client.GetFromJsonAsync<ApiEnvelope<IReadOnlyCollection<PaymentResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/billing/payments?take=50");
        Assert.Single(paymentList!.Data);
        Assert.Equal(payment.ReceiptNumber, paymentList.Data.Single().ReceiptNumber);
        SetHeaders(client, tenant, FoundationPermissions.BillingPaymentsReceive);
        var originalReceiptHash = payment.ReceiptSnapshot.PayloadHash;
        var paymentReplay = await PostCreated<PaymentResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/billing/payments", paymentRequest);
        Assert.True(paymentReplay.IsReplay);
        Assert.Equal(payment.ReceiptNumber, paymentReplay.ReceiptNumber);

        SetHeaders(client, tenant, FoundationPermissions.BillingPaymentsView);
        var forbiddenAllocation = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/billing/payments/{payment.Id}/allocations",
            new AllocatePaymentRequest(Guid.NewGuid(), invoice.Id, 150m, payment.Version, invoice.Version));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenAllocation.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.BillingPaymentsAllocate);
        payment = await PostCreated<PaymentResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/billing/payments/{payment.Id}/allocations",
            new AllocatePaymentRequest(Guid.NewGuid(), invoice.Id, 150m, payment.Version, invoice.Version));
        Assert.Equal(150m, payment.AllocatedAmount);
        Assert.Equal(150m, payment.UnallocatedAmount);
        Assert.Single(payment.Allocations);
        Assert.Equal(originalReceiptHash, payment.ReceiptSnapshot.PayloadHash);

        var stale = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/billing/payments/{payment.Id}/allocations",
            new AllocatePaymentRequest(Guid.NewGuid(), invoice.Id, 10m, 1, 1));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        payment = await PostCreated<PaymentResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/billing/payments/{payment.Id}/allocations",
            new AllocatePaymentRequest(Guid.NewGuid(), invoice.Id, 100m, payment.Version, 2));
        Assert.Equal(250m, payment.AllocatedAmount);
        Assert.Equal(50m, payment.UnallocatedAmount);
        Assert.Equal("Confirmed", payment.Status);

        SetHeaders(client, tenant, FoundationPermissions.BillingInvoicesView);
        var loadedInvoice = await client.GetFromJsonAsync<ApiEnvelope<InvoiceResponse>>(
            $"/api/v1/branches/{tenant.BranchId}/billing/invoices/{invoice.Id}");
        Assert.Equal("Paid", loadedInvoice!.Data.Status);
        Assert.Equal(0m, loadedInvoice.Data.Balance);
        Assert.Equal(invoice.Snapshot.PayloadHash, loadedInvoice.Data.Snapshot.PayloadHash);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            var snapshot = await db.FinancialDocumentSnapshots.IgnoreQueryFilters().FirstAsync();
            db.Entry(snapshot).State = EntityState.Deleted;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        var otherTenant = await ProvisionAsync(client, "Other Billing Clinic");
        SetHeaders(client, otherTenant, FoundationPermissions.BillingInvoicesView);
        var crossTenant = await client.GetAsync(
            $"/api/v1/branches/{otherTenant.BranchId}/billing/invoices/{invoice.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, crossTenant.StatusCode);
    }

    private static async Task<(PatientResponse Patient, ServiceResponse Service)> CreatePatientAndServiceAsync(
        HttpClient client, TenantProvisioningResponse tenant)
    {
        SetHeaders(client, tenant, FoundationPermissions.CatalogManage);
        var service = await PostCreated<ServiceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services",
            new CreateServiceRequest("BILL-PHY", "Physiotherapy session", null, 30));
        SetHeaders(client, tenant, FoundationPermissions.PatientsRegister);
        var patient = await PostCreated<PatientResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/patients",
            new RegisterPatientRequest(Guid.NewGuid(),
                new(null, "Billing", null, "Patient", new DateOnly(1990, 1, 1), false, "Other"), null,
                [new("Email", "billing.patient@example.invalid", true)], [], [], [], null));
        return (patient, service);
    }

    private static async Task<T> PostCreated<T>(HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"Expected Created from {url}, received {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>())!.Data;
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client, string name)
    {
        Clear(client);
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest(name, $"billing-{Guid.NewGuid():N}",
                "owner@example.invalid", "Delhi Main", "DEL01"));
        var submitted = (await submittedResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!.Data;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8501");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var approved = await client.PostAsJsonAsync(
            $"/api/v1/platform/tenant-applications/{submitted.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Version));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        return (await approved.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static void SetHeaders(HttpClient client, TenantProvisioningResponse tenant, params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8502");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
        if (permissions.Length > 0)
            client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[]
                 { "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator" })
            client.DefaultRequestHeaders.Remove(header);
    }
}
