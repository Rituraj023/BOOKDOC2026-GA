using BookDoc2026.Api.Context;
using BookDoc2026.Api.Middleware;
using BookDoc2026.Api.Security;
using BookDoc2026.Application;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Security;
using BookDoc2026.DocumentService;
using BookDoc2026.ErrorHandling;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Messaging;
using BookDoc2026.Messaging;
using BookDoc2026.Shared.Kernel.Security;
using BookDoc2026.Worker;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
builder.AddBookDocServiceDefaults();
const string testOrBearerScheme = "BookDocTestingOrBearer";

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();
builder.Services.AddScoped<ICorrelationContext, HttpCorrelationContext>();
builder.Services.AddScoped<HttpPublicIdDecoder>();
builder.Services.AddBookDocApplication();
builder.Services.AddBookDocInfrastructure(builder.Configuration);
builder.Services.AddBookDocErrorHandling();
builder.Services.AddSingleton<IExceptionErrorMapper, DomainExceptionErrorMapper>();
builder.Services.AddBookDocMessaging();
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IMessageProvider, DevelopmentEmailMessageProvider>();
    builder.Services.AddSingleton<IProviderCallbackVerifier, DevelopmentCallbackVerifier>();
}
builder.Services.AddBookDocDocumentService();
builder.Services.AddBookDocWorker();

builder.Services
    .AddAuthentication(options =>
    {
        var defaultScheme = builder.Environment.IsEnvironment("Testing")
            ? testOrBearerScheme
            : JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = defaultScheme;
        options.DefaultChallengeScheme = defaultScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(
        DevelopmentHeaderAuthenticationHandler.SchemeName,
        _ => { })
    .AddPolicyScheme(testOrBearerScheme, testOrBearerScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
                : DevelopmentHeaderAuthenticationHandler.SchemeName;
    });

builder.Services.AddAuthorization(options =>
{
    AddPermissionPolicy(FoundationPermissions.TenantsRegister, platformOnly: true);
    AddPermissionPolicy(FoundationPermissions.TenantsApprove, platformOnly: true);
    AddPermissionPolicy(FoundationPermissions.BranchesView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.BranchesConfigurationManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.BranchAdministratorsManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.UsersManage, platformOnly: true);
    AddPermissionPolicy(FoundationPermissions.PatientsRegister, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.PatientsSearch, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.PatientsView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.PatientsUpdate, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.CatalogView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.CatalogManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.ResourcesView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.ResourcesManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.ResourceStatusManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.StakeholdersView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.StakeholdersManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.StakeholderDocumentsManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.SchedulingAvailabilityView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.SchedulingAvailabilityManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.SchedulingHoldsCreate, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.SchedulingHoldsRelease, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.MessageDeliveriesView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.MessageTemplatesView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.MessageTemplatesManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.MessageTemplatesPublish, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.CommunicationPreferencesView, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.CommunicationPreferencesManage, platformOnly: false);
    AddPermissionPolicy(FoundationPermissions.ProviderCallbacksView, platformOnly: false);

    void AddPermissionPolicy(string permission, bool platformOnly)
    {
        options.AddPolicy(permission, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(BookDocClaimTypes.Permission, permission);
            if (platformOnly)
            {
                policy.RequireClaim(BookDocClaimTypes.IsPlatformOperator, bool.TrueString);
            }
        });
    }
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapBookDocDefaultEndpoints();
app.Run();

public partial class Program;
