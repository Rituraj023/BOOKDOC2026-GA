using BookDoc2026.Templates;

namespace BookDoc2026.Messaging;

public enum MessageChannel
{
    Email = 1,
    Sms = 2,
    WhatsApp = 3,
    Push = 4
}

public sealed record MessageRecipient(string Address, string? DisplayName = null);

public sealed record MessageDispatchRequest(
    TemplateScope Scope,
    MessageChannel Channel,
    string TemplateKey,
    string Culture,
    MessageRecipient Recipient,
    IReadOnlyDictionary<string, string?> Values,
    Guid IdempotencyKey,
    int? TemplateVersion = null);

public sealed record MessageEnvelope(
    TemplateScope Scope,
    MessageChannel Channel,
    MessageRecipient Recipient,
    string TemplateKey,
    int TemplateVersion,
    string Culture,
    string? Subject,
    string Body,
    string ContentType,
    Guid IdempotencyKey);

public sealed record MessageDeliveryResult(
    bool Accepted,
    string? ProviderMessageId = null,
    bool IsTransientFailure = false,
    string? ErrorCode = null,
    string? ProviderCode = null,
    int? TemplateVersion = null)
{
    public static MessageDeliveryResult Sent(string providerMessageId) =>
        new(true, providerMessageId);

    public static MessageDeliveryResult Failed(string errorCode, bool transient) =>
        new(false, null, transient, errorCode);
}

public sealed class MessageConfigurationException(
    string errorCode,
    string safeMessage) : InvalidOperationException(safeMessage)
{
    public string ErrorCode { get; } = errorCode;
}

public sealed record ProviderCallbackVerificationResult(
    string ExternalEventId,
    string ProviderMessageId,
    string Status,
    DateTimeOffset OccurredUtc,
    string SignatureKeyId,
    string PayloadSha256);

public sealed class ProviderCallbackVerificationException(
    string errorCode,
    string safeMessage) : InvalidOperationException(safeMessage)
{
    public string ErrorCode { get; } = errorCode;
}
