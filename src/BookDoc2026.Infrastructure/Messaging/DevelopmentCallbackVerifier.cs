using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Messaging;
using Microsoft.Extensions.Configuration;

namespace BookDoc2026.Infrastructure.Messaging;

public sealed class DevelopmentCallbackVerifier(IConfiguration configuration) : IProviderCallbackVerifier
{
    public const string SigningKeyConfiguration = "Messaging:DevelopmentCallbackSigningKey";
    public string ProviderCode => "development-email";

    public ProviderCallbackVerificationResult Verify(ReadOnlyMemory<byte> payload, string signature)
    {
        if (payload.Length is 0 or > 65_536)
            throw Invalid("callback_payload_invalid", "The callback payload is invalid.");
        var key = configuration[SigningKeyConfiguration];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw Invalid("callback_verifier_not_configured", "The callback verifier is not configured.");
        if (!signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            || signature.Length != 71)
            throw Invalid("callback_signature_invalid", "The callback signature is invalid.");

        byte[] supplied;
        try { supplied = Convert.FromHexString(signature[7..]); }
        catch (FormatException) { throw Invalid("callback_signature_invalid", "The callback signature is invalid."); }
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), payload.Span);
        if (!CryptographicOperations.FixedTimeEquals(supplied, expected))
            throw Invalid("callback_signature_invalid", "The callback signature is invalid.");

        DevelopmentCallbackPayload? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<DevelopmentCallbackPayload>(payload.Span,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            throw Invalid("callback_payload_invalid", "The callback payload is invalid.");
        }

        if (parsed is null
            || string.IsNullOrWhiteSpace(parsed.EventId)
            || parsed.EventId.Length > 200
            || string.IsNullOrWhiteSpace(parsed.ProviderMessageId)
            || parsed.ProviderMessageId.Length > 200
            || string.IsNullOrWhiteSpace(parsed.Status)
            || parsed.Status.Length > 40
            || parsed.OccurredUtc == default)
            throw Invalid("callback_payload_invalid", "The callback payload is invalid.");

        return new ProviderCallbackVerificationResult(
            parsed.EventId.Trim(),
            parsed.ProviderMessageId.Trim(),
            parsed.Status.Trim(),
            parsed.OccurredUtc,
            "development-v1",
            Convert.ToHexString(SHA256.HashData(payload.Span)));
    }

    private static ProviderCallbackVerificationException Invalid(string code, string message) => new(code, message);

    private sealed record DevelopmentCallbackPayload(
        string EventId,
        string ProviderMessageId,
        string Status,
        DateTimeOffset OccurredUtc);
}
