using BookDoc2026.Blazor.UI.Billing;
using BookDoc2026.Contracts.Billing;

namespace BookDoc2026.UnitTests;

public sealed class CashierBillingDraftTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void InvoiceDraft_CalculatesLinesAndRetainsRequestIdentityUntilReset()
    {
        var draft = new CashierBillingDraft();
        var requestId = draft.InvoiceRequestId;
        draft.AddInvoiceLine("service-1", "PHY", "Physiotherapy", 2, 125.50m);

        var first = draft.BuildInvoiceRequest("patient-1", null, null, "INR", null);
        var retry = draft.BuildInvoiceRequest("patient-1", null, null, "INR", null);

        Assert.Equal(251m, draft.InvoiceDraftTotal);
        Assert.Equal(requestId, first.RequestId);
        Assert.Equal(first.RequestId, retry.RequestId);
        Assert.Equal(first.PatientId, retry.PatientId);
        Assert.Equal(first.Lines, retry.Lines);
        draft.Reset();
        Assert.NotEqual(requestId, draft.InvoiceRequestId);
        Assert.Empty(draft.InvoiceLines);
    }

    [Fact]
    public void PaymentDraft_PreservesSplitTenderAndRejectsMutationAfterConfirmation()
    {
        var draft = new CashierBillingDraft();
        draft.AddTender("Cash", 100m, null, null);
        draft.AddTender("Upi", 150m, "safe-reference", null);
        var request = draft.BuildPaymentRequest("patient-1", "INR", null);

        Assert.Equal(250m, draft.TenderTotal);
        Assert.Equal(2, request.Tenders.Count);
        draft.CompletePayment(Payment(250m));
        Assert.Throws<InvalidOperationException>(() => draft.AddTender("Cash", 1m, null, null));
    }

    [Fact]
    public void AllocationDraft_FitsBothBalancesAndRotatesIdentityOnlyAfterCompletion()
    {
        var draft = new CashierBillingDraft();
        draft.CompleteInvoice(Invoice(200m));
        draft.CompletePayment(Payment(150m));
        var requestId = draft.AllocationRequestId;

        var request = draft.BuildAllocationRequest(150m);

        Assert.Equal(requestId, request.RequestId);
        Assert.Equal(1, request.ExpectedInvoiceVersion);
        Assert.Throws<InvalidOperationException>(() => draft.BuildAllocationRequest(151m));
        draft.CompleteAllocation(Payment(150m, 150m, 2), Invoice(200m, 150m, 2));
        Assert.NotEqual(requestId, draft.AllocationRequestId);
        var firstPaymentRequestId = draft.PaymentRequestId;
        draft.BeginAdditionalPayment();
        Assert.Null(draft.Payment);
        Assert.NotEqual(firstPaymentRequestId, draft.PaymentRequestId);
        Assert.Equal(50m, draft.Invoice!.Balance);
    }

    private static FinancialDocumentSnapshotResponse Snapshot(string kind) =>
        new("snapshot", kind, "number", "V1", new string('a', 64), Now);

    private static InvoiceResponse Invoice(decimal total, decimal allocated = 0, long version = 1) =>
        new("invoice", "patient-1", null, null, "INV-1", "INR", total, 0, 0, total, allocated,
            total - allocated, allocated == total ? "Paid" : allocated > 0 ? "PartPaid" : "Issued", "V1", Now,
            "actor", version, false, [], Snapshot("Invoice"));

    private static PaymentResponse Payment(decimal total, decimal allocated = 0, long version = 1) =>
        new("payment", "patient-1", "RCT-1", "INR", total, allocated, total - allocated,
            allocated == total ? "FullyAllocated" : "Confirmed", Now, "actor", version, false, [], [],
            Snapshot("Receipt"));
}
