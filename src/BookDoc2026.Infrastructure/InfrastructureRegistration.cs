using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Infrastructure.Security;
using BookDoc2026.Infrastructure.Messaging;
using BookDoc2026.Infrastructure.Time;
using BookDoc2026.Infrastructure.Worker;
using BookDoc2026.Messaging;
using BookDoc2026.Templates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace BookDoc2026.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddBookDocInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        AddBookDocInfrastructureCore(services, configuration, null);

    public static IServiceCollection AddBookDocInfrastructureForTesting(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> configureDatabase) =>
        AddBookDocInfrastructureCore(services, configuration, configureDatabase);

    private static IServiceCollection AddBookDocInfrastructureCore(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDatabase)
    {
        var nodeId = configuration.GetValue<int?>("NumericIds:NodeId") ?? 0;
        NumericId.ConfigureNode(nodeId);
        var keyRingPath = configuration["DataProtection:KeyRingPath"];
        if (string.IsNullOrWhiteSpace(keyRingPath))
        {
            keyRingPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BookDoc2026",
                "DataProtectionKeys");
        }
        var keyDirectory = new DirectoryInfo(Path.IsPathRooted(keyRingPath)
            ? keyRingPath
            : Path.Combine(AppContext.BaseDirectory, keyRingPath));
        keyDirectory.Create();
        services.AddDataProtection()
            .SetApplicationName(configuration["DataProtection:ApplicationName"] ?? "BookDoc2026")
            .PersistKeysToFileSystem(keyDirectory);
        services.AddSingleton<IPublicIdCodec, DataProtectionPublicIdCodec>();
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.Configure<OutboxProcessingOptions>(configuration.GetSection(OutboxProcessingOptions.SectionName));

        services.AddDbContext<BookDocDbContext>(options =>
        {
            if (configureDatabase is not null)
            {
                configureDatabase(options);
                return;
            }

            var connectionString = configuration.GetConnectionString("BookDoc")
                ?? throw new InvalidOperationException("ConnectionStrings:BookDoc is required.");
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(BookDocDbContext).Assembly.FullName)
                    .EnableRetryOnFailure());
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<BookDocDbContext>()
            .AddDefaultTokenProviders();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        services.AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        if (!string.IsNullOrWhiteSpace(token)
                            && context.HttpContext.Request.Path.StartsWithSegments("/hubs/queue"))
                            context.Token = token;
                        return Task.CompletedTask;
                    }
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerValidator = (issuer, _, _) =>
                        string.Equals(
                            issuer,
                            configuration[$"{JwtSettings.SectionName}:Issuer"] ?? jwtSettings.Issuer,
                            StringComparison.Ordinal)
                            ? issuer
                            : throw new SecurityTokenInvalidIssuerException("The token issuer is invalid."),
                    AudienceValidator = (audiences, _, _) =>
                    {
                        var expected = configuration[$"{JwtSettings.SectionName}:Audience"] ?? jwtSettings.Audience;
                        return !string.IsNullOrWhiteSpace(expected)
                            && audiences.Contains(expected, StringComparer.Ordinal);
                    },
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                    {
                        var key = configuration[$"{JwtSettings.SectionName}:Key"];
                        return string.IsNullOrWhiteSpace(key) || System.Text.Encoding.UTF8.GetByteCount(key) < 32
                            ? []
                            : [new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key))];
                    }
                };
            });

        services.AddScoped<IFoundationRepository, FoundationRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IStakeholderRepository, StakeholderRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<ISchedulingRepository, SchedulingRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IEncounterRepository, EncounterRepository>();
        services.AddScoped<IInvestigationRepository, InvestigationRepository>();
        services.AddScoped<IRadiologyStudyRepository, RadiologyStudyRepository>();
        services.AddScoped<IPhysiotherapyRepository, PhysiotherapyRepository>();
        services.AddScoped<IPractitionerRepository, PractitionerRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<ITemplateCatalog, EfTemplateCatalog>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIdentityAdministrationService, IdentityAdministrationService>();
        services.AddScoped<IOutboxMessageHandler, TenantApprovedMessageHandler>();
        services.AddScoped<IOutboxMessageHandler, ProviderCallbackReceivedHandler>();
        services.AddScoped<IOutboxMessageHandler, BookingConfirmedMessageHandler>();
        services.AddScoped<BookingLifecycleMessageDispatcher>();
        services.AddScoped<IOutboxMessageHandler, BookingCancelledMessageHandler>();
        services.AddScoped<IOutboxMessageHandler, BookingRescheduledMessageHandler>();
        services.AddScoped<IOutboxMessageHandler, WaitlistPromotedMessageHandler>();
        return services;
    }
}
