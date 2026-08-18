using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class PublicIdCodecTests
{
    [Fact]
    public async Task ProtectedId_RoundTripsOnlyForItsResourceTypeAndTenant()
    {
        await using var factory = new BookDocApiFactory();
        using var scope = factory.Services.CreateScope();
        var codec = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
        var tenantId = NumericId.Next();
        var patientId = NumericId.Next();
        var protectedId = codec.Encode(PublicIdKind.Patient, patientId, tenantId);

        Assert.Equal(patientId, codec.Decode(PublicIdKind.Patient, protectedId, tenantId));
        Assert.Throws<InvalidPublicIdException>(() =>
            codec.Decode(PublicIdKind.BookableResource, protectedId, tenantId));
        Assert.Throws<InvalidPublicIdException>(() =>
            codec.Decode(PublicIdKind.Patient, protectedId, NumericId.Next()));
    }

    [Fact]
    public async Task ProtectedId_RejectsTampering()
    {
        await using var factory = new BookDocApiFactory();
        using var scope = factory.Services.CreateScope();
        var codec = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
        var tenantId = NumericId.Next();
        var protectedId = codec.Encode(PublicIdKind.Patient, NumericId.Next(), tenantId);
        var tampered = protectedId[..^1] + (protectedId[^1] == 'A' ? 'B' : 'A');

        Assert.Throws<InvalidPublicIdException>(() =>
            codec.Decode(PublicIdKind.Patient, tampered, tenantId));
    }
}
