using BookDoc2026.Admin.Components;
using BookDoc2026.Admin.Security;
using BookDoc2026.Blazor.UI;
using BookDoc2026.Client;

var builder = WebApplication.CreateBuilder(args);
builder.AddBookDocServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AdminSessionState>();
builder.Services.AddScoped<IBookDocTokenStore>(services => services.GetRequiredService<AdminSessionState>());
builder.Services.Configure<AdminNetworkOptions>(builder.Configuration.GetSection(AdminNetworkOptions.SectionName));
builder.Services.AddBookDocApiClients(builder.Configuration);
builder.Services.AddBookDocBlazorUi();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");
app.UseMiddleware<AdminNetworkRestrictionMiddleware>();
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapBookDocDefaultEndpoints();
app.Run();
