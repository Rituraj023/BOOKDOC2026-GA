using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Communications;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Worker;

public sealed class ProviderCallbackReceivedHandler(
    BookDocDbContext dbContext,
    IClock clock) : IOutboxMessageHandler
{
    public string MessageType => ProviderCallbackReceivedOutboxPayload.MessageType;

    public async Task HandleAsync(OutboxMessageContext context, CancellationToken cancellationToken)
    {
        ProviderCallbackReceivedOutboxPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<ProviderCallbackReceivedOutboxPayload>(context.PayloadJson)
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new OutboxDispatchException("invalid_callback_payload", false, "The callback work item is invalid.");
        }

        if (context.TenantId != payload.TenantId || context.BranchId != payload.BranchId)
            throw new OutboxDispatchException("callback_scope_mismatch", false, "The callback work item scope is invalid.");
        var callback = await dbContext.ProviderCallbackInboxes
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(candidate => candidate.Id == payload.CallbackInboxId
                && candidate.TenantId == payload.TenantId, cancellationToken)
            ?? throw new OutboxDispatchException("callback_not_found", false, "The callback work item was not found.");
        if (callback.Status == ProviderCallbackInboxStatus.Processed) return;

        var attempt = await dbContext.MessageDeliveryAttempts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == callback.DeliveryAttemptId
                && candidate.TenantId == callback.TenantId, cancellationToken)
            ?? throw new OutboxDispatchException("callback_delivery_not_found", false, "The callback delivery was not found.");
        var alreadyRecorded = await dbContext.MessageDeliveryStatusEvents
            .IgnoreQueryFilters()
            .AnyAsync(status => status.ProviderCode == callback.ProviderCode
                && status.ExternalEventId == callback.ExternalEventId, cancellationToken);
        if (!alreadyRecorded)
        {
            await dbContext.MessageDeliveryStatusEvents.AddAsync(
                MessageDeliveryStatusEvent.Record(callback, attempt.OperationId, clock.UtcNow),
                cancellationToken);
        }
        callback.MarkProcessed(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
