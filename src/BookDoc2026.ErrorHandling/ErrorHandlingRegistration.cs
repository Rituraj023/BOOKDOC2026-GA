using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookDoc2026.ErrorHandling;

public static class ErrorHandlingRegistration
{
    public static IServiceCollection AddBookDocErrorHandling(this IServiceCollection services)
    {
        services.TryAddSingleton<ExceptionErrorResolver>();
        return services;
    }
}
