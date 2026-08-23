using System.Security.Cryptography;
using System.Text;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Billing;

public sealed class FinancialDocumentSnapshot : TenantScopedEntity
{
    private FinancialDocumentSnapshot() { }

    public long BranchId { get; private set; }
    public FinancialDocumentKind Kind { get; private set; }
    public long SourceId { get; private set; }
    public string DocumentNumber { get; private set; } = string.Empty;
    public string SchemaVersion { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;

    public static FinancialDocumentSnapshot Capture(long tenantId, long branchId, FinancialDocumentKind kind,
        long sourceId, string documentNumber, string schemaVersion, string canonicalPayloadJson, DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0 || sourceId <= 0 || !Enum.IsDefined(kind))
            throw new DomainRuleException("Financial document snapshot scope is invalid.");
        var payload = BillingMoney.Required(canonicalPayloadJson, 16000, "Snapshot payload");
        var snapshot = new FinancialDocumentSnapshot
        {
            TenantId = tenantId,
            BranchId = branchId,
            Kind = kind,
            SourceId = sourceId,
            DocumentNumber = BillingMoney.Required(documentNumber, 60, "Document number"),
            SchemaVersion = BillingMoney.Required(schemaVersion, 40, "Snapshot schema version"),
            PayloadJson = payload,
            PayloadHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))
        };
        snapshot.StampCreated(now);
        return snapshot;
    }
}
