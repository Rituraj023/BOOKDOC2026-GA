using BookDoc2026.Blazor.UI;
using BookDoc2026.Client;
using BookDoc2026.Portal;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped<IBookDocTokenStore, InMemoryBookDocTokenStore>();
builder.Services.AddBookDocApiClients(builder.Configuration);
builder.Services.AddBookDocBlazorUi();
await builder.Build().RunAsync();
