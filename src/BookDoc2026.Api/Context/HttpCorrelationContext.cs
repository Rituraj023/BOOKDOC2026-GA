using BookDoc2026.Application.Abstractions;

namespace BookDoc2026.Api.Context;

public sealed class HttpCorrelationContext(IHttpContextAccessor httpContextAccessor) : ICorrelationContext
{
    public string CorrelationId =>
        httpContextAccessor.HttpContext?.TraceIdentifier ?? $"api-{Guid.NewGuid():N}";
}
