using BookDoc2026.Blazor.UI;
using BookDoc2026.Client;
using BookDoc2026.Portal;
using BookDoc2026.Portal.Security;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PortalSessionState>();
builder.Services.AddSingleton<IBookDocTokenStore>(services => services.GetRequiredService<PortalSessionState>());
builder.Services.AddBookDocApiClients(builder.Configuration);
builder.Services.AddBookDocBlazorUi();
await builder.Build().RunAsync();
