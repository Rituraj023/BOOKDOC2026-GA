using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BookDoc2026.Client;

public static class BookDocClientRegistration
{
    public static IServiceCollection AddBookDocApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BookDocClientOptions>(configuration.GetSection(BookDocClientOptions.SectionName));
        services.AddScoped<AuthorizedApiMessageHandler>();
        services.AddHttpClient<AuthApiClient>(ConfigureClient);
        services.AddHttpClient<BookDocApiClient>(ConfigureClient)
            .AddHttpMessageHandler<AuthorizedApiMessageHandler>();
        return services;
    }

    private static void ConfigureClient(IServiceProvider services, HttpClient client)
    {
        var options = services.GetRequiredService<IOptions<BookDocClientOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    }
}
