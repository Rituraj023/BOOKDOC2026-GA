using BookDoc2026.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookDoc2026.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddBookDocMessaging(this IServiceCollection services)
    {
        services.AddBookDocTemplates();
        services.TryAddScoped<IMessageDispatcher, MessageDispatcher>();
        return services;
    }
}
