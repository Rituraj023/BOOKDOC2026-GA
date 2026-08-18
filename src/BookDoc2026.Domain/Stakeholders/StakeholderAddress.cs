using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Stakeholders;

public sealed class StakeholderAddress : TenantScopedEntity
{
    private StakeholderAddress()
    {
    }

    public long StakeholderId { get; private set; }

    public string AddressType { get; private set; } = "PRIMARY";

    public string Line1 { get; private set; } = string.Empty;

    public string? Line2 { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string StateCode { get; private set; } = string.Empty;

    public string PostalCode { get; private set; } = string.Empty;

    public string CountryCode { get; private set; } = "IN";

    public bool IsPrimary { get; private set; }

    public static StakeholderAddress Create(
        long tenantId,
        long stakeholderId,
        string addressType,
        string line1,
        string? line2,
        string city,
        string stateCode,
        string postalCode,
        bool isPrimary,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(addressType) || string.IsNullOrWhiteSpace(line1)
            || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(stateCode)
            || string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainRuleException("Address type, line, city, state and postal code are required.");
        }

        var digits = string.Concat(postalCode.Where(char.IsDigit));
        if (digits.Length != 6)
        {
            throw new DomainRuleException("Indian postal code must contain six digits.");
        }

        var address = new StakeholderAddress
        {
            TenantId = tenantId,
            StakeholderId = stakeholderId,
            AddressType = addressType.Trim().ToUpperInvariant(),
            Line1 = line1.Trim(),
            Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim(),
            City = city.Trim(),
            StateCode = stateCode.Trim().ToUpperInvariant(),
            PostalCode = digits,
            IsPrimary = isPrimary
        };
        address.StampCreated(now);
        return address;
    }
}
