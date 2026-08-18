namespace BookDoc2026.Application.Abstractions;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}
