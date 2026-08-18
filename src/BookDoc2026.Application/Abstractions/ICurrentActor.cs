namespace BookDoc2026.Application.Abstractions;

public interface ICurrentActor
{
    long ActorId { get; }

    long? TenantId { get; }

    bool IsPlatformOperator { get; }

    IReadOnlySet<long> BranchIds { get; }

    bool HasPermission(string permission);
}
