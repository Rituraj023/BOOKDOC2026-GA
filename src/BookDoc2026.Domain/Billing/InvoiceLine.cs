using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Billing;

public sealed class InvoiceLine : TenantScopedEntity
{
    private InvoiceLine() { }

    public long InvoiceId { get; private set; }
    public long ServiceId { get; private set; }
    public string ServiceCodeSnapshot { get; private set; } = string.Empty;
    public string DescriptionSnapshot { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }

    public static InvoiceLine Create(Invoice invoice, long serviceId, string serviceCode, string description,
        int quantity, decimal unitPrice, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (serviceId <= 0 || quantity is < 1 or > 1000)
            throw new DomainRuleException("Invoice line service and quantity are invalid.");
        var price = BillingMoney.Amount(unitPrice, "Unit price", true);
        var total = BillingMoney.Amount(price * quantity, "Line total", true);
        var line = new InvoiceLine
        {
            TenantId = invoice.TenantId,
            InvoiceId = invoice.Id,
            ServiceId = serviceId,
            ServiceCodeSnapshot = BillingMoney.Required(serviceCode, 40, "Service code"),
            DescriptionSnapshot = BillingMoney.Required(description, 300, "Line description"),
            Quantity = quantity,
            UnitPrice = price,
            LineTotal = total
        };
        line.StampCreated(now);
        return line;
    }
}
