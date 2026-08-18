using BookDoc2026.Messaging;

namespace BookDoc2026.Infrastructure.Messaging;

public sealed class DevelopmentEmailMessageProvider : IMessageProvider
{
    public string ProviderCode => "development-email";

    public MessageChannel Channel => MessageChannel.Email;

    public ValueTask<MessageDeliveryResult> SendAsync(
        MessageEnvelope message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(MessageDeliveryResult.Sent($"dev-{message.IdempotencyKey:N}"));
    }
}
