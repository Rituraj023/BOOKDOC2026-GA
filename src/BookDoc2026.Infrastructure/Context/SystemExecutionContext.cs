using BookDoc2026.Application.Abstractions;

namespace BookDoc2026.Infrastructure.Context;

public sealed class SystemExecutionContext : ICurrentActor, ICorrelationContext
{
    private static readonly IReadOnlySet<long> NoBranches = new HashSet<long>();

    public long ActorId => 0;

    public long? TenantId => null;

    public bool IsPlatformOperator => true;

    public IReadOnlySet<long> BranchIds => NoBranches;

    public string CorrelationId => $"worker-{Guid.NewGuid():N}";

    public bool HasPermission(string permission) => true;
}
