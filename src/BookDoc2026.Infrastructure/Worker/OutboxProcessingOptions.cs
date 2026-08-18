namespace BookDoc2026.Infrastructure.Worker;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; init; } = 20;

    public int MaxAttempts { get; init; } = 5;

    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);
}
