namespace BookDoc2026.Domain.Foundation;

public sealed class Organization : Common.TenantScopedEntity
{
    private Organization()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public static Organization Create(long tenantId, string name, DateTimeOffset now)
    {
        var organization = new Organization { TenantId = tenantId, Name = name.Trim() };
        organization.StampCreated(now);
        return organization;
    }
}
