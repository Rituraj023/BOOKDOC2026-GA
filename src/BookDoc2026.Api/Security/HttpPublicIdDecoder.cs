using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.Api.Security;

public sealed class HttpPublicIdDecoder(IPublicIdCodec codec, ICurrentActor actor)
{
    public long Platform(PublicIdKind kind, string value) => codec.Decode(kind, value);

    public long Tenant(PublicIdKind kind, string value)
    {
        if (actor.TenantId is not > 0) throw new ForbiddenException("A tenant scope is required.");
        return codec.Decode(kind, value, actor.TenantId.Value);
    }

    public long? TenantOptional(PublicIdKind kind, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Tenant(kind, value);
}
