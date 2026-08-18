using BookDoc2026.Domain.Identity;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string requiredConfirmation = "CREATE_FIRST_PLATFORM_OPERATOR";

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services.AddBookDocInfrastructure(builder.Configuration);

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
var confirmation = configuration["Bootstrap:Confirmation"];
var email = configuration["Bootstrap:Email"]?.Trim().ToLowerInvariant();
var displayName = configuration["Bootstrap:DisplayName"]?.Trim();
var password = configuration["Bootstrap:Password"];

if (!string.Equals(confirmation, requiredConfirmation, StringComparison.Ordinal))
{
    return Fail($"Set Bootstrap:Confirmation to {requiredConfirmation} for this one-time operation.");
}

if (string.IsNullOrWhiteSpace(email)
    || string.IsNullOrWhiteSpace(displayName)
    || string.IsNullOrWhiteSpace(password))
{
    return Fail("Bootstrap:Email, Bootstrap:DisplayName and Bootstrap:Password are required.");
}

var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
if (!await dbContext.Database.CanConnectAsync())
{
    return Fail("The BOOKDOC database is not reachable.");
}

var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
if (pendingMigrations.Length > 0)
{
    return Fail("Apply the reviewed database migrations before bootstrapping the platform operator.");
}

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
if (await roleManager.FindByNameAsync(BookDocRoleNames.PlatformOperator) is null)
{
    return Fail("The PlatformOperator role is missing. Verify the Identity migration and seed data.");
}

var platformOperators = await userManager.GetUsersInRoleAsync(BookDocRoleNames.PlatformOperator);
if (platformOperators.Count > 0)
{
    if (platformOperators.Any(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine("The requested platform operator is already configured; no change was made.");
        return 0;
    }

    return Fail("A platform operator already exists. Use normal authenticated Identity administration.");
}

if (await userManager.FindByEmailAsync(email) is not null)
{
    return Fail("The bootstrap email already belongs to a non-platform user; no elevation was performed.");
}

var now = DateTimeOffset.UtcNow;
var operatorUser = new ApplicationUser
{
    UserName = email,
    Email = email,
    DisplayName = displayName,
    EmailConfirmed = true,
    IsActive = true,
    CreatedUtc = now,
    ModifiedUtc = now
};
var createResult = await userManager.CreateAsync(operatorUser, password);
if (!createResult.Succeeded)
{
    return Fail(string.Join(" ", createResult.Errors.Select(error => error.Description)));
}

var roleResult = await userManager.AddToRoleAsync(operatorUser, BookDocRoleNames.PlatformOperator);
if (!roleResult.Succeeded)
{
    await userManager.DeleteAsync(operatorUser);
    return Fail(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
}

Console.WriteLine("The first BOOKDOC platform operator was created. Remove the bootstrap secrets and confirmation from the environment now.");
return 0;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
