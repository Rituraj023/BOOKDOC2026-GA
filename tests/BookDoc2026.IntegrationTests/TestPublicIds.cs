using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Foundation;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

internal sealed record TestTenant(long TenantId, long BranchId);

internal static class TestPublicIds
{
    public static TestTenant DecodeTenant(IServiceProvider services, TenantProvisioningResponse tenant)
    {
        var codec = services.GetRequiredService<IPublicIdCodec>();
        var tenantId = codec.Decode(PublicIdKind.Tenant, tenant.TenantId);
        return new TestTenant(
            tenantId,
            codec.Decode(PublicIdKind.Branch, tenant.BranchId, tenantId));
    }

    public static long Decode(
        IServiceProvider services,
        PublicIdKind kind,
        string protectedId,
        long tenantId) =>
        services.GetRequiredService<IPublicIdCodec>().Decode(kind, protectedId, tenantId);

    public static long DecodePlatform(
        IServiceProvider services,
        PublicIdKind kind,
        string protectedId) =>
        services.GetRequiredService<IPublicIdCodec>().Decode(kind, protectedId);
}
