using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Billing;

public sealed class PaymentTender : TenantScopedEntity
{
    private PaymentTender() { }

    public long PaymentId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? Narration { get; private set; }

    public static PaymentTender Create(Payment payment, PaymentMethod method, decimal amount,
        string? externalReference, string? narration, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payment);
        if (!Enum.IsDefined(method)) throw new DomainRuleException("Payment method is not supported.");
        var tender = new PaymentTender
        {
            TenantId = payment.TenantId,
            PaymentId = payment.Id,
            Method = method,
            Amount = BillingMoney.Amount(amount, "Tender amount"),
            ExternalReference = BillingMoney.Optional(externalReference, 120),
            Narration = BillingMoney.Optional(narration, 250)
        };
        tender.StampCreated(now);
        return tender;
    }
}
