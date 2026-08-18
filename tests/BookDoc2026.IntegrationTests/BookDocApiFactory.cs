using BookDoc2026.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookDoc2026.IntegrationTests;

internal sealed class BookDocApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"bookdoc-api-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "BookDoc2026.Tests",
                ["Jwt:Audience"] = "BookDoc2026.TestClients",
                ["Jwt:Key"] = "bookdoc-tests-only-signing-key-32-bytes-minimum",
                ["Jwt:AccessTokenMinutes"] = "5",
                ["Jwt:RefreshTokenDays"] = "1",
                ["Outbox:PollInterval"] = "01:00:00",
                ["Messaging:DevelopmentCallbackSigningKey"] = "bookdoc-tests-only-callback-key-32-bytes-minimum"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BookDocDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<BookDocDbContext>>();
            services.RemoveAll<BookDocDbContext>();
            services.AddDbContext<BookDocDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
