namespace BookDoc2026.Domain.Common;

public abstract class Entity
{
    public long Id { get; protected set; } = NumericId.Next();

    public DateTimeOffset CreatedUtc { get; protected set; }

    public DateTimeOffset ModifiedUtc { get; protected set; }

    protected void StampCreated(DateTimeOffset now)
    {
        CreatedUtc = now;
        ModifiedUtc = now;
    }

    protected void StampModified(DateTimeOffset now) => ModifiedUtc = now;
}

public interface ITenantScoped
{
    long TenantId { get; }
}

public abstract class TenantScopedEntity : Entity, ITenantScoped
{
    public long TenantId { get; protected set; }
}
