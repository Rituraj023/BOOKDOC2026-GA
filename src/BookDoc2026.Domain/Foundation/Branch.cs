using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Foundation;

public sealed class Branch : TenantScopedEntity
{
    private Branch()
    {
    }

    public long OrganizationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string TimeZoneId { get; private set; } = "Asia/Kolkata";

    public static Branch Create(
        long tenantId,
        long organizationId,
        string code,
        string name,
        DateTimeOffset now)
    {
        var branch = new Branch
        {
            TenantId = tenantId,
            OrganizationId = organizationId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim()
        };
        branch.StampCreated(now);
        return branch;
    }
}
