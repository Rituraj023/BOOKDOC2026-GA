using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Stakeholders;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Context;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Messaging;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string confirmationText = "MANAGE_BOOKDOC_DEVELOPMENT_BROWSER_FIXTURES";
const string tenantSlug = "bookdoc-browser-fixture";
const string receptionEmail = "reception.browser@bookdoc.invalid";
const string technicianEmail = "technician.browser@bookdoc.invalid";
const string receptionRole = "DevelopmentBrowserReception";
const string technicianRole = "DevelopmentBrowserTechnician";

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services.AddSingleton<SystemExecutionContext>();
builder.Services.AddSingleton<ICurrentActor>(services => services.GetRequiredService<SystemExecutionContext>());
builder.Services.AddSingleton<ICorrelationContext>(services => services.GetRequiredService<SystemExecutionContext>());
builder.Services.AddBookDocInfrastructure(builder.Configuration);
builder.Services.AddBookDocMessaging();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
var action = configuration["Fixture:Action"]?.Trim().ToLowerInvariant();
var confirmation = configuration["Fixture:Confirmation"];
var password = configuration["Fixture:Password"];

if (!environment.IsDevelopment()) return Fail("Development fixtures require DOTNET_ENVIRONMENT=Development.");
if (!string.Equals(confirmation, confirmationText, StringComparison.Ordinal))
    return Fail($"Set Fixture:Confirmation to {confirmationText}.");
if (action is not ("create" or "remove")) return Fail("Fixture:Action must be create or remove.");
if (action == "create" && string.IsNullOrWhiteSpace(password)) return Fail("Fixture:Password is required for create.");

var connectionString = configuration.GetConnectionString("BookDoc")
    ?? throw new InvalidOperationException("ConnectionStrings:BookDoc is required.");
var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
if (!databaseName.EndsWith("_Dev", StringComparison.OrdinalIgnoreCase))
    return Fail("Fixture commands are restricted to a database whose name ends with _Dev.");

var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
if (!await db.Database.CanConnectAsync()) return Fail("The development database is not reachable.");
if ((await db.Database.GetPendingMigrationsAsync()).Any()) return Fail("Apply reviewed migrations before managing fixtures.");

var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

return action == "create"
    ? await CreateAsync(db, users, roles, password!)
    : await RemoveAsync(db);

static async Task<int> CreateAsync(
    BookDocDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles,
    string password)
{
    if (await db.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Slug == tenantSlug)
        || await users.FindByEmailAsync(receptionEmail) is not null
        || await users.FindByEmailAsync(technicianEmail) is not null)
        return Fail("Browser fixtures already exist. Run the remove action before recreating them.");

    try
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var now = DateTimeOffset.UtcNow;
            var tenant = Tenant.Activate("BOOKDOC Browser Fixture Clinic", tenantSlug, now);
            var organization = Organization.Create(tenant.Id, "Browser Fixture Organization", now);
            var branch = Branch.Create(tenant.Id, organization.Id, "BROWSER", "Browser Fixture Delhi", now);
            var branchConfiguration = BranchConfiguration.CreateDefault(tenant.Id, branch.Id, branch.Code, now);
            var stakeholder = Stakeholder.CreatePerson(tenant.Id, "Aarav Browser Patient", now);
            var person = StakeholderPerson.Create(tenant.Id, stakeholder.Id, null, "Aarav", null, "Browser Patient",
                new DateOnly(1990, 1, 15), false, AdministrativeSex.Male, now);
            var contact = StakeholderContactPoint.Create(tenant.Id, stakeholder.Id, ContactPointType.Mobile,
                "+91 9876543210", true, now);
            var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("BOOKDOC-BROWSER-FIXTURE-PATIENT")));
            var patient = Patient.Register(tenant.Id, branch.Id, Guid.NewGuid(), payloadHash, stakeholder.Id, null, now);
            var servicePoint = ImagingServicePoint.Create(tenant.Id, branch.Id, "XR-BROWSER",
                "Browser Test X-ray", ImagingModality.XRay, null, now);

            db.AddRange(tenant, organization, branch, branchConfiguration, stakeholder, person, contact, patient, servicePoint);
            await db.SaveChangesAsync();

            await EnsureRoleAsync(roles, receptionRole,
                FoundationPermissions.PatientsSearch,
                FoundationPermissions.QueuesCheckIn);
            await EnsureRoleAsync(roles, technicianRole,
                FoundationPermissions.QueuesView,
                FoundationPermissions.QueuesCall,
                FoundationPermissions.QueuesProgress,
                FoundationPermissions.QueuesCancel,
                FoundationPermissions.QueuesDisplayView);

            await CreateUserAsync(db, users, receptionEmail, "Browser Reception", receptionRole, password,
                tenant.Id, organization.Id, branch.Id, now);
            await CreateUserAsync(db, users, technicianEmail, "Browser Technician", technicianRole, password,
                tenant.Id, organization.Id, branch.Id, now);

            await transaction.CommitAsync();
            Console.WriteLine("Development browser fixtures created.");
            Console.WriteLine($"Reception user: {receptionEmail}");
            Console.WriteLine($"Technician user: {technicianEmail}");
            Console.WriteLine("Synthetic patient search: Aarav Browser Patient or 9876");
            Console.WriteLine("Run the explicit remove action after browser validation.");
            return 0;
        });
    }
    catch (Exception exception)
    {
        return Fail($"Fixture creation failed: {exception.Message}");
    }
}

static async Task<int> RemoveAsync(BookDocDbContext db)
{
    try
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var tenant = await db.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Slug == tenantSlug);
            if (tenant is null)
            {
                Console.WriteLine("Development browser fixture tenant is absent; no tenant data was removed.");
            }
            else
            {
                var tenantId = tenant.Id;
                await db.QueueTicketEvents.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.QueueTickets.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ImagingServicePoints.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.AuditEvents.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Patients.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.StakeholderContactPoints.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.StakeholderPersons.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Stakeholders.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.UserScopes.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.BranchConfigurations.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Branches.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Organizations.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Tenants.IgnoreQueryFilters().Where(item => item.Id == tenantId).ExecuteDeleteAsync();
                Console.WriteLine("Development browser fixture tenant data removed.");
            }

            var normalizedEmails = new[] { receptionEmail.ToUpperInvariant(), technicianEmail.ToUpperInvariant() };
            var userIds = await db.Users
                .Where(user => user.NormalizedEmail != null && normalizedEmails.Contains(user.NormalizedEmail))
                .Select(user => user.Id)
                .ToListAsync();
            await db.Set<ApplicationUserClaim>().Where(item => userIds.Contains(item.UserId)).ExecuteDeleteAsync();
            await db.Set<ApplicationUserLogin>().Where(item => userIds.Contains(item.UserId)).ExecuteDeleteAsync();
            await db.Set<ApplicationUserToken>().Where(item => userIds.Contains(item.UserId)).ExecuteDeleteAsync();
            await db.Set<ApplicationUserRole>().Where(item => userIds.Contains(item.UserId)).ExecuteDeleteAsync();
            await db.Users.Where(user => userIds.Contains(user.Id)).ExecuteDeleteAsync();

            var normalizedRoles = new[] { receptionRole.ToUpperInvariant(), technicianRole.ToUpperInvariant() };
            var roleIds = await db.Roles
                .Where(role => role.NormalizedName != null && normalizedRoles.Contains(role.NormalizedName))
                .Select(role => role.Id)
                .ToListAsync();
            await db.Set<ApplicationRoleClaim>().Where(item => roleIds.Contains(item.RoleId)).ExecuteDeleteAsync();
            await db.Roles.Where(role => roleIds.Contains(role.Id)).ExecuteDeleteAsync();
            await transaction.CommitAsync();
            Console.WriteLine("Development browser fixture users and roles removed.");
            return 0;
        });
    }
    catch (Exception exception)
    {
        return Fail($"Fixture removal failed: {exception.Message}");
    }
}

static async Task EnsureRoleAsync(RoleManager<ApplicationRole> roles, string name, params string[] permissions)
{
    var role = new ApplicationRole { Name = name, IsActive = true, IsPlatformRole = false };
    Ensure(await roles.CreateAsync(role));
    foreach (var permission in permissions)
        Ensure(await roles.AddClaimAsync(role, new Claim(BookDocClaimTypes.Permission, permission)));
}

static async Task CreateUserAsync(
    BookDocDbContext db,
    UserManager<ApplicationUser> users,
    string email,
    string displayName,
    string role,
    string password,
    long tenantId,
    long organizationId,
    long branchId,
    DateTimeOffset now)
{
    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        DisplayName = displayName,
        EmailConfirmed = true,
        IsActive = true,
        CreatedUtc = now,
        ModifiedUtc = now
    };
    Ensure(await users.CreateAsync(user, password));
    Ensure(await users.AddToRoleAsync(user, role));
    db.UserScopes.Add(ApplicationUserScope.Create(user.Id, tenantId, organizationId, branchId, true, now));
    await db.SaveChangesAsync();
}

static void Ensure(IdentityResult result)
{
    if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
