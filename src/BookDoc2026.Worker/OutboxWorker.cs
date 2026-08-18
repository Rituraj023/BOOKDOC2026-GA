using BookDoc2026.Application.Abstractions;
using BookDoc2026.ErrorHandling;
using BookDoc2026.Infrastructure.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookDoc2026.Worker;

public sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessingOptions> options,
    ExceptionErrorResolver errors,
    TimeProvider timeProvider,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                var completed = await processor.ProcessBatchAsync(stoppingToken);
                if (completed > 0)
                {
                    logger.LogInformation("Completed {CompletedCount} outbox messages.", completed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                var error = errors.Resolve(exception);
                logger.LogError(
                    exception,
                    "Outbox polling cycle failed. ErrorCode={ErrorCode} Category={ErrorCategory} Transient={IsTransient}",
                    error.Code,
                    error.Category,
                    error.IsTransient);
            }

            await Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken);
        }
    }
}
