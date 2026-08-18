using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookDoc2026.Infrastructure.Worker;

public sealed class OutboxProcessor(
    BookDocDbContext dbContext,
    IEnumerable<IOutboxMessageHandler> handlers,
    IClock clock,
    IOptions<OutboxProcessingOptions> options,
    ILogger<OutboxProcessor> logger) : IOutboxProcessor
{
    private readonly Dictionary<string, IOutboxMessageHandler> _handlers = handlers.ToDictionary(
        handler => handler.MessageType,
        StringComparer.Ordinal);
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = clock.UtcNow;
        var messages = await dbContext.OutboxMessages
            .IgnoreQueryFilters()
            .Where(message =>
                (message.Status == OutboxStatus.Pending || message.Status == OutboxStatus.RetryScheduled
                    || (message.Status == OutboxStatus.Processing && message.LeaseUntilUtc <= now))
                && message.NextAttemptUtc <= now)
            .OrderBy(message => message.CreatedUtc)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        var completed = 0;
        foreach (var message in messages)
        {
            try
            {
                message.Claim(_workerId, clock.UtcNow, settings.LeaseDuration);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.Entry(message).State = EntityState.Detached;
                continue;
            }

            try
            {
                if (!_handlers.TryGetValue(message.MessageType, out var handler))
                {
                    throw new InvalidOperationException($"No handler is registered for {message.MessageType}.");
                }

                await handler.HandleAsync(new OutboxMessageContext(
                    message.Id,
                    message.TenantId,
                    message.BranchId,
                    message.MessageType,
                    message.PayloadJson,
                    message.OperationId,
                    message.CorrelationId,
                    message.AttemptCount), cancellationToken);
                message.Complete(_workerId, clock.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                completed++;
            }
            catch (OutboxDispatchException exception)
            {
                logger.LogWarning(
                    "Outbox message {MessageId} was not delivered. ErrorCode={ErrorCode} Transient={IsTransient} CorrelationId={CorrelationId}",
                    message.Id,
                    exception.ErrorCode,
                    exception.IsTransient,
                    message.CorrelationId);
                message.Fail(
                    _workerId,
                    exception.ErrorCode,
                    exception.IsTransient,
                    clock.UtcNow,
                    settings.MaxAttempts);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(
                    exception,
                    "Outbox message {MessageId} failed unexpectedly. CorrelationId={CorrelationId}",
                    message.Id,
                    message.CorrelationId);
                message.Fail(_workerId, "internal_error", true, clock.UtcNow, settings.MaxAttempts);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return completed;
    }
}
