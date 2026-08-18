using BookDoc2026.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.Maui.UI;

public static class BookDocMauiUiRegistration
{
    public static IServiceCollection AddBookDocMauiUi(this IServiceCollection services)
    {
        services.AddSingleton<IBookDocTokenStore, MauiBookDocTokenStore>();
        return services;
    }
}
