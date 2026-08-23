using BookDoc2026.Domain.Billing;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.UnitTests;

public sealed class BillingDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Invoice_ComputesServerOwnedBasicTotalsAndCannotResetThem()
    {
        var invoice = Invoice.Issue(1, 2, 3, null, null, Guid.NewGuid(), "hash", "DEL-INV", "INR",
            "BASIC-NET-V1", null, 9, Now);
        var lines = new[]
        {
            InvoiceLine.Create(invoice, 10, "PHY", "Physiotherapy", 2, 125.50m, Now),
            InvoiceLine.Create(invoice, 11, "CONSULT", "Consultation", 1, 250m, Now)
        };
        invoice.SetIssuedTotals(lines);
        Assert.Equal(501m, invoice.Total);
        Assert.Equal(0m, invoice.DiscountTotal);
        Assert.Equal(0m, invoice.TaxTotal);
        Assert.Throws<DomainRuleException>(() => invoice.SetIssuedTotals(lines));
    }

    [Fact]
    public void Payment_SumsSplitTenderAndRejectsReset()
    {
        var payment = Payment.Confirm(1, 2, 3, Guid.NewGuid(), "hash", "DEL-RCT", "INR", null, 9, Now);
        var tenders = new[]
        {
            PaymentTender.Create(payment, PaymentMethod.Cash, 100m, null, null, Now),
            PaymentTender.Create(payment, PaymentMethod.Upi, 150m, "UPI-REF", null, Now)
        };
        payment.SetConfirmedAmount(tenders);
        Assert.Equal(250m, payment.Amount);
        Assert.Throws<DomainRuleException>(() => payment.SetConfirmedAmount(tenders));
    }

    [Fact]
    public void Allocation_UpdatesPaymentAndInvoiceBalancesAtomically()
    {
        var invoice = Invoice.Issue(1, 2, 3, null, null, Guid.NewGuid(), "invoice-hash", "INV", "INR",
            "BASIC-NET-V1", null, 9, Now);
        var line = InvoiceLine.Create(invoice, 10, "PHY", "Physiotherapy", 1, 200m, Now);
        invoice.SetIssuedTotals([line]);
        var payment = Payment.Confirm(1, 2, 3, Guid.NewGuid(), "payment-hash", "RCT", "INR", null, 9, Now);
        payment.SetConfirmedAmount([PaymentTender.Create(payment, PaymentMethod.Cash, 200m, null, null, Now)]);
        _ = PaymentAllocation.Create(payment, invoice, Guid.NewGuid(), "allocation-hash", 200m, 1, 1, 9, Now);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(PaymentStatus.FullyAllocated, payment.Status);
        Assert.Equal(0m, invoice.Balance);
        Assert.Equal(0m, payment.UnallocatedAmount);
    }

    [Fact]
    public void Allocation_RejectsMismatchedPatientCurrencyAndOverAllocation()
    {
        var invoice = Invoice.Issue(1, 2, 3, null, null, Guid.NewGuid(), "i", "INV", "INR",
            "BASIC-NET-V1", null, 9, Now);
        invoice.SetIssuedTotals([InvoiceLine.Create(invoice, 10, "PHY", "Physio", 1, 100m, Now)]);
        var wrongPatient = Payment.Confirm(1, 2, 4, Guid.NewGuid(), "p", "RCT", "INR", null, 9, Now);
        wrongPatient.SetConfirmedAmount([PaymentTender.Create(wrongPatient, PaymentMethod.Cash, 200m, null, null, Now)]);
        Assert.Throws<DomainRuleException>(() => PaymentAllocation.Create(wrongPatient, invoice, Guid.NewGuid(),
            "a", 50m, 1, 1, 9, Now));
        var payment = Payment.Confirm(1, 2, 3, Guid.NewGuid(), "p2", "RCT", "INR", null, 9, Now);
        payment.SetConfirmedAmount([PaymentTender.Create(payment, PaymentMethod.Cash, 200m, null, null, Now)]);
        Assert.Throws<DomainRuleException>(() => PaymentAllocation.Create(payment, invoice, Guid.NewGuid(),
            "a2", 101m, 1, 1, 9, Now));
        Assert.Equal(0m, payment.AllocatedAmount);
        Assert.Equal(0m, invoice.AllocatedAmount);
        Assert.Equal(1, payment.Version);
        Assert.Equal(1, invoice.Version);
    }

    [Fact]
    public void FinancialSnapshot_HashIsDeterministicAndPayloadIsImmutableByDesign()
    {
        var first = FinancialDocumentSnapshot.Capture(1, 2, FinancialDocumentKind.Receipt, 3, "RCT-1",
            "V1", "{\"amount\":100}", Now);
        var second = FinancialDocumentSnapshot.Capture(1, 2, FinancialDocumentKind.Receipt, 4, "RCT-2",
            "V1", "{\"amount\":100}", Now);
        Assert.Equal(first.PayloadHash, second.PayloadHash);
        Assert.Equal(64, first.PayloadHash.Length);
    }
}
