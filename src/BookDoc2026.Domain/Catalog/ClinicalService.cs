using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Catalog;

public sealed class ClinicalService : TenantScopedEntity
{
    private ClinicalService()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int DefaultDurationMinutes { get; private set; }

    public CatalogItemStatus Status { get; private set; }

    public long Version { get; private set; } = 1;

    public static ClinicalService Create(
        long tenantId,
        string code,
        string name,
        string? description,
        int defaultDurationMinutes,
        DateTimeOffset now)
    {
        ValidateDuration(defaultDurationMinutes);
        var service = new ClinicalService
        {
            TenantId = tenantId,
            Code = CatalogCode.Normalize(code, "Service"),
            Name = CatalogCode.RequiredName(name, "Service", 200),
            Description = CleanDescription(description),
            DefaultDurationMinutes = defaultDurationMinutes,
            Status = CatalogItemStatus.Active
        };
        service.StampCreated(now);
        return service;
    }

    public void Update(
        long expectedVersion,
        string name,
        string? description,
        int defaultDurationMinutes,
        bool isActive,
        DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        ValidateDuration(defaultDurationMinutes);
        Name = CatalogCode.RequiredName(name, "Service", 200);
        Description = CleanDescription(description);
        DefaultDurationMinutes = defaultDurationMinutes;
        Status = isActive ? CatalogItemStatus.Active : CatalogItemStatus.Inactive;
        Version++;
        StampModified(now);
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException("The service changed after it was loaded.");
        }
    }

    private static void ValidateDuration(int duration)
    {
        if (duration is < 5 or > 1440)
        {
            throw new DomainRuleException("Service duration must be between 5 and 1440 minutes.");
        }
    }

    private static string? CleanDescription(string? value)
    {
        if (value?.Trim().Length > 1000)
        {
            throw new DomainRuleException("Service description cannot exceed 1000 characters.");
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
