using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Communications;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Messaging;

namespace BookDoc2026.Application.Communications;

public sealed class ProviderCallbackIngressService(
    ICommunicationRepository repository,
    IEnumerable<IProviderCallbackVerifier> verifiers,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlationContext)
{
    public async Task<ProviderCallbackAcceptedResponse> ReceiveAsync(
        string providerCode,
        ReadOnlyMemory<byte> payload,
        string signature,
        CancellationToken cancellationToken)
    {
        var verifier = verifiers.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase));
        if (verifier is null)
            throw new UnauthorizedException("The provider callback could not be authenticated.");

        ProviderCallbackVerificationResult verified;
        try { verified = verifier.Verify(payload, signature); }
        catch (ProviderCallbackVerificationException)
        {
            throw new UnauthorizedException("The provider callback could not be authenticated.");
        }

        var existing = await repository.FindCallbackAsync(verifier.ProviderCode, verified.ExternalEventId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.PayloadSha256, verified.PayloadSha256, StringComparison.OrdinalIgnoreCase))
                throw new ConcurrencyConflictException("The provider event identifier was replayed with different content.");
            return new ProviderCallbackAcceptedResponse(
                publicIds.Encode(PublicIdKind.ProviderCallbackInbox, existing.Id, existing.TenantId),
                existing.Status.ToString(),
                true);
        }

        var attempt = await repository.FindDeliveryAttemptForCallbackAsync(
            verifier.ProviderCode,
            verified.ProviderMessageId,
            cancellationToken)
            ?? throw new NotFoundException("The provider callback does not match an accepted delivery.");
        var callback = ProviderCallbackInbox.Receive(
            attempt.TenantId,
            attempt.BranchId,
            attempt.Id,
            verifier.ProviderCode,
            verified.ExternalEventId,
            verified.ProviderMessageId,
            ParseStatus(verified.Status),
            verified.OccurredUtc,
            verified.SignatureKeyId,
            verified.PayloadSha256,
            clock.UtcNow);
        var operationId = Guid.NewGuid();
        var outbox = OutboxMessage.Enqueue(
            attempt.TenantId,
            attempt.BranchId,
            ProviderCallbackReceivedOutboxPayload.MessageType,
            JsonSerializer.Serialize(new ProviderCallbackReceivedOutboxPayload(
                callback.Id,
                callback.TenantId,
                callback.BranchId)),
            clock.UtcNow,
            operationId,
            correlationContext.CorrelationId);
        await repository.AddCallbackAsync(callback, cancellationToken);
        await repository.AddOutboxMessageAsync(outbox, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new ProviderCallbackAcceptedResponse(
            publicIds.Encode(PublicIdKind.ProviderCallbackInbox, callback.Id, callback.TenantId),
            callback.Status.ToString(),
            false);
    }

    private static ProviderDeliveryStatus ParseStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "delivered" => ProviderDeliveryStatus.Delivered,
        "failed" => ProviderDeliveryStatus.Failed,
        "bounced" => ProviderDeliveryStatus.Bounced,
        _ => throw new DomainRuleException("The provider callback delivery status is not supported.")
    };
}

public sealed record ProviderCallbackReceivedOutboxPayload(
    long CallbackInboxId,
    long TenantId,
    long? BranchId)
{
    public const string MessageType = "Communications.ProviderCallbackReceived.v1";
}
