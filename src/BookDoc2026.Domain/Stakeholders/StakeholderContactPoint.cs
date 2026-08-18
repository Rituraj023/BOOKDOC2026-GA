using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Stakeholders;

public sealed class StakeholderContactPoint : TenantScopedEntity
{
    private StakeholderContactPoint()
    {
    }

    public long StakeholderId { get; private set; }

    public ContactPointType Type { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public string NormalizedValue { get; private set; } = string.Empty;

    public bool IsPrimary { get; private set; }

    public bool IsVerified { get; private set; }

    public static StakeholderContactPoint Create(
        long tenantId,
        long stakeholderId,
        ContactPointType type,
        string value,
        bool isPrimary,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException("Contact value is required.");
        }

        var contact = new StakeholderContactPoint
        {
            TenantId = tenantId,
            StakeholderId = stakeholderId,
            Type = type,
            Value = value.Trim(),
            NormalizedValue = Normalize(type, value),
            IsPrimary = isPrimary,
            IsVerified = false
        };
        contact.StampCreated(now);
        return contact;
    }

    public static string Normalize(ContactPointType type, string value) => type switch
    {
        ContactPointType.Email when value.Contains('@', StringComparison.Ordinal) => value.Trim().ToLowerInvariant(),
        ContactPointType.Mobile when string.Concat(value.Where(char.IsDigit)) is { Length: >= 10 and <= 15 } mobile => mobile,
        ContactPointType.Email => throw new DomainRuleException("Email contact is invalid."),
        ContactPointType.Mobile => throw new DomainRuleException("Mobile contact must contain 10 to 15 digits."),
        _ => throw new DomainRuleException("Contact type is not supported.")
    };
}
