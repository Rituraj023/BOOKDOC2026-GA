using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Stakeholders;

public sealed class StakeholderCorporate : TenantScopedEntity
{
    private StakeholderCorporate()
    {
    }

    public long StakeholderId { get; private set; }

    public string LegalName { get; private set; } = string.Empty;

    public string? TradeName { get; private set; }

    public string? RegistrationNumber { get; private set; }

    public long Version { get; private set; } = 1;

    public static StakeholderCorporate Create(
        long tenantId,
        long stakeholderId,
        string legalName,
        string? tradeName,
        string? registrationNumber,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(legalName) || legalName.Trim().Length > 240)
        {
            throw new DomainRuleException("Corporate legal name is required and cannot exceed 240 characters.");
        }

        var corporate = new StakeholderCorporate
        {
            TenantId = tenantId,
            StakeholderId = stakeholderId,
            LegalName = legalName.Trim(),
            TradeName = CleanOptional(tradeName, 240, "Trade name"),
            RegistrationNumber = CleanOptional(registrationNumber, 100, "Registration number")
        };
        corporate.StampCreated(now);
        return corporate;
    }

    private static string? CleanOptional(string? value, int maxLength, string label)
    {
        if (value?.Trim().Length > maxLength)
        {
            throw new DomainRuleException($"{label} cannot exceed {maxLength} characters.");
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
