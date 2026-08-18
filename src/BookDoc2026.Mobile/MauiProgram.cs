using BookDoc2026.Blazor.UI;
using BookDoc2026.Client;
using BookDoc2026.Maui.UI;
using Microsoft.Extensions.Logging;

namespace BookDoc2026.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Configuration["Api:BaseUrl"] =
            Environment.GetEnvironmentVariable("Api__BaseUrl") ?? "https://localhost:7001";
        builder.Services.AddBookDocMauiUi();
        builder.Services.AddBookDocApiClients(builder.Configuration);
        builder.Services.AddBookDocBlazorUi();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
