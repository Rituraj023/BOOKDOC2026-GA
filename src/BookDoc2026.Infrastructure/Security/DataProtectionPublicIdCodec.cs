using System.Globalization;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Common;
using Microsoft.AspNetCore.DataProtection;

namespace BookDoc2026.Infrastructure.Security;

public sealed class DataProtectionPublicIdCodec(IDataProtectionProvider provider) : IPublicIdCodec
{
    private const string Version = "v1";

    public string Encode(PublicIdKind kind, long id, long? tenantId = null)
    {
        if (id <= 0) throw new InvalidPublicIdException("A positive internal identifier is required.");
        var scope = ResolveScopeForEncoding(kind, id, tenantId);
        var payload = string.Create(CultureInfo.InvariantCulture, $"{Version}|{scope}|{id}");
        return Protector(kind).Protect(payload);
    }

    public long Decode(PublicIdKind kind, string protectedId, long? expectedTenantId = null)
    {
        if (string.IsNullOrWhiteSpace(protectedId)) throw Invalid();
        if (kind is not PublicIdKind.Tenant and not PublicIdKind.TenantApplication and not PublicIdKind.IdentitySubject
            && expectedTenantId is not > 0)
            throw Invalid();
        try
        {
            var payload = Protector(kind).Unprotect(protectedId);
            var parts = payload.Split('|');
            if (parts.Length != 3 || parts[0] != Version
                || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var scope)
                || !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                || id <= 0)
                throw Invalid();

            if (kind == PublicIdKind.Tenant && scope != id) throw Invalid();
            if (kind == PublicIdKind.TenantApplication && scope != 0) throw Invalid();
            if (kind == PublicIdKind.IdentitySubject && scope != 0) throw Invalid();
            if (expectedTenantId.HasValue && scope != expectedTenantId.Value) throw Invalid();
            return id;
        }
        catch (InvalidPublicIdException) { throw; }
        catch { throw Invalid(); }
    }

    private IDataProtector Protector(PublicIdKind kind) =>
        provider.CreateProtector($"BookDoc2026.PublicId.{Version}.{kind}");

    private static long ResolveScopeForEncoding(PublicIdKind kind, long id, long? tenantId) => kind switch
    {
        PublicIdKind.TenantApplication => 0,
        PublicIdKind.IdentitySubject => 0,
        PublicIdKind.Tenant => id,
        _ when tenantId is > 0 => tenantId.Value,
        _ => throw new InvalidPublicIdException("A tenant scope is required for this identifier type.")
    };

    private static InvalidPublicIdException Invalid() =>
        new("The protected identifier is invalid, expired, belongs to another tenant, or is for another resource type.");
}
