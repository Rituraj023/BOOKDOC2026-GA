namespace BookDoc2026.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
