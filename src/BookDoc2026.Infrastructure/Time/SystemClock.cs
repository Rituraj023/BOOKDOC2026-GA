using BookDoc2026.Application.Abstractions;

namespace BookDoc2026.Infrastructure.Time;

public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
