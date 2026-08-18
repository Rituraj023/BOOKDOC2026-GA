using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Stakeholders;

public sealed class StakeholderIdentifier : TenantScopedEntity
{
    private StakeholderIdentifier()
    {
    }

    public long StakeholderId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public string NormalizedValue { get; private set; } = string.Empty;

    public string Issuer { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static StakeholderIdentifier Create(
        long tenantId,
        long stakeholderId,
        string type,
        string value,
        string? issuer,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException("Identifier type and value are required.");
        }

        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            throw new DomainRuleException("Identifier value must contain a letter or digit.");
        }

        var identifier = new StakeholderIdentifier
        {
            TenantId = tenantId,
            StakeholderId = stakeholderId,
            Type = type.Trim().ToUpperInvariant(),
            Value = value.Trim(),
            NormalizedValue = normalized,
            Issuer = string.IsNullOrWhiteSpace(issuer) ? "LOCAL" : issuer.Trim().ToUpperInvariant(),
            IsActive = true
        };
        identifier.StampCreated(now);
        return identifier;
    }

    public static string Normalize(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();
}
