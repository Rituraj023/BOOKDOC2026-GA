namespace BookDoc2026.Domain.Billing;

public enum InvoiceStatus
{
    Issued = 1,
    PartPaid = 2,
    Paid = 3
}

public enum PaymentStatus
{
    Confirmed = 1,
    FullyAllocated = 2
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    Cheque = 3,
    BankTransfer = 4,
    Upi = 5,
    Other = 6
}

public enum FinancialDocumentKind
{
    Invoice = 1,
    Receipt = 2
}
