namespace BookDoc2026.Domain.Foundation;

public sealed class Tenant : Common.Entity
{
    private Tenant()
    {
    }

    public string LegalName { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public TenantStatus Status { get; private set; }

    public static Tenant Activate(string legalName, string slug, DateTimeOffset now)
    {
        var tenant = new Tenant
        {
            LegalName = legalName,
            Slug = slug,
            Status = TenantStatus.Active
        };
        tenant.StampCreated(now);
        return tenant;
    }
}
