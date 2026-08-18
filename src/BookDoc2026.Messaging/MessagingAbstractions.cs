namespace BookDoc2026.Messaging;

public interface IMessageProvider
{
    string ProviderCode { get; }

    MessageChannel Channel { get; }

    ValueTask<MessageDeliveryResult> SendAsync(
        MessageEnvelope message,
        CancellationToken cancellationToken = default);
}

public interface IMessageDispatcher
{
    ValueTask<MessageDeliveryResult> DispatchAsync(
        MessageDispatchRequest request,
        CancellationToken cancellationToken = default);
}

public interface IProviderCallbackVerifier
{
    string ProviderCode { get; }

    ProviderCallbackVerificationResult Verify(
        ReadOnlyMemory<byte> payload,
        string signature);
}
