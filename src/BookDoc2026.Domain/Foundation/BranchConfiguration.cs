using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Foundation;

public sealed class BranchConfiguration : TenantScopedEntity
{
    private BranchConfiguration()
    {
    }

    public long BranchId { get; private set; }

    public string? LogoUrl { get; private set; }

    public string InvoicePrefix { get; private set; } = string.Empty;

    public string ReceiptPrefix { get; private set; } = string.Empty;

    public string? EmailSender { get; private set; }

    public string? WhatsAppNumber { get; private set; }

    public CommunicationVerificationStatus CommunicationVerificationStatus { get; private set; }

    public long Version { get; private set; } = 1;

    public static BranchConfiguration CreateDefault(long tenantId, long branchId, string branchCode, DateTimeOffset now)
    {
        var configuration = new BranchConfiguration
        {
            TenantId = tenantId,
            BranchId = branchId,
            InvoicePrefix = $"{branchCode}-INV",
            ReceiptPrefix = $"{branchCode}-RCT",
            CommunicationVerificationStatus = CommunicationVerificationStatus.NotRequired
        };
        configuration.StampCreated(now);
        return configuration;
    }

    public void Update(
        long expectedVersion,
        string? logoUrl,
        string invoicePrefix,
        string receiptPrefix,
        string? emailSender,
        string? whatsAppNumber,
        DateTimeOffset now)
    {
        if (Version != expectedVersion)
        {
            throw new ConcurrencyConflictException("Branch configuration has changed. Refresh and try again.");
        }

        InvoicePrefix = RequiredPrefix(invoicePrefix, nameof(invoicePrefix));
        ReceiptPrefix = RequiredPrefix(receiptPrefix, nameof(receiptPrefix));
        LogoUrl = NormalizeOptional(logoUrl, 500);
        EmailSender = NormalizeOptional(emailSender, 320)?.ToLowerInvariant();
        WhatsAppNumber = NormalizeOptional(whatsAppNumber, 30);
        CommunicationVerificationStatus = EmailSender is null && WhatsAppNumber is null
            ? CommunicationVerificationStatus.NotRequired
            : CommunicationVerificationStatus.Pending;
        Version++;
        StampModified(now);
    }

    private static string RequiredPrefix(string value, string field)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 30
            || normalized.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
        {
            throw new DomainRuleException($"{field} must contain 1-30 letters, numbers or hyphens.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException($"Value must not exceed {maxLength} characters.");
        }

        return normalized;
    }
}
