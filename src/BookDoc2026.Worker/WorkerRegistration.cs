using BookDoc2026.ErrorHandling;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.Worker;

public static class WorkerRegistration
{
    public static IServiceCollection AddBookDocWorker(this IServiceCollection services)
    {
        services.AddBookDocErrorHandling();
        services.AddHostedService<OutboxWorker>();
        return services;
    }
}
