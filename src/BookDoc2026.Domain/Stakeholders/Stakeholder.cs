using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Stakeholders;

public sealed class Stakeholder : TenantScopedEntity
{
    private Stakeholder()
    {
    }

    public StakeholderType Type { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public StakeholderStatus Status { get; private set; }

    public long Version { get; private set; } = 1;

    public static Stakeholder CreatePerson(long tenantId, string displayName, DateTimeOffset now) =>
        Create(tenantId, StakeholderType.Person, displayName, now);

    public static Stakeholder CreateCorporate(long tenantId, string displayName, DateTimeOffset now) =>
        Create(tenantId, StakeholderType.Corporate, displayName, now);

    public void Rename(long expectedVersion, string displayName, DateTimeOffset now)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException("The stakeholder changed after it was loaded.");
        }

        DisplayName = RequiredDisplayName(displayName);
        Version++;
        StampModified(now);
    }

    private static Stakeholder Create(
        long tenantId,
        StakeholderType type,
        string displayName,
        DateTimeOffset now)
    {
        if (tenantId == 0)
        {
            throw new DomainRuleException("Tenant is required for a stakeholder.");
        }

        var stakeholder = new Stakeholder
        {
            TenantId = tenantId,
            Type = type,
            DisplayName = RequiredDisplayName(displayName),
            Status = StakeholderStatus.Active
        };
        stakeholder.StampCreated(now);
        return stakeholder;
    }

    private static string RequiredDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 240)
        {
            throw new DomainRuleException("Stakeholder display name is required and cannot exceed 240 characters.");
        }

        return value.Trim();
    }
}
