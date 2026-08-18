using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class AuthApiTests
{
    [Fact]
    public async Task PlatformLogin_IssuesJwtAuthorizesAndRotatesRefreshToken()
    {
        await using var factory = new BookDocApiFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "platform@example.invalid",
                Email = "platform@example.invalid",
                DisplayName = "Platform Operator",
                EmailConfirmed = true
            };
            Assert.True((await userManager.CreateAsync(user, "Test-Only-Password1!")).Succeeded);
            Assert.True((await userManager.AddToRoleAsync(user, BookDocRoleNames.PlatformOperator)).Succeeded);
        }

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("platform@example.invalid", "Test-Only-Password1!"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var first = (await loginResponse.Content.ReadFromJsonAsync<ApiEnvelope<AuthSessionResponse>>())!.Data;
        Assert.True(first.IsPlatformOperator);
        Assert.Contains("Tenants.Register", first.Permissions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken);
        var authorized = await client.PostAsJsonAsync(
            "/api/v1/platform/tenant-applications",
            new SubmitTenantApplicationRequest(
                "JWT Clinic",
                $"jwt-clinic-{Guid.NewGuid():N}",
                "jwt-clinic@example.invalid",
                "Main Branch",
                "JWT01"));
        Assert.True(
            authorized.StatusCode == HttpStatusCode.Created,
            $"Expected Created but received {authorized.StatusCode}. WWW-Authenticate: {string.Join("; ", authorized.Headers.WwwAuthenticate)}");

        client.DefaultRequestHeaders.Authorization = null;
        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshSessionRequest(first.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var second = (await refreshResponse.Content.ReadFromJsonAsync<ApiEnvelope<AuthSessionResponse>>())!.Data;
        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        var replayResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshSessionRequest(first.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
    }

    [Fact]
    public async Task ClinicAdministratorLogin_UsesDurableDefaultScopeAndAuthorizesOnlyThatBranch()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "9001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add(
            "X-Permissions",
            $"{FoundationPermissions.TenantsRegister},{FoundationPermissions.TenantsApprove}");

        var submittedResponse = await client.PostAsJsonAsync(
            "/api/v1/platform/tenant-applications",
            new SubmitTenantApplicationRequest(
                "Scoped Clinic",
                $"scoped-clinic-{Guid.NewGuid():N}",
                "scoped-clinic@example.invalid",
                "Delhi Branch",
                "DEL01"));
        var submitted = (await submittedResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!.Data;
        var approvedResponse = await client.PostAsJsonAsync(
            $"/api/v1/platform/tenant-applications/{submitted.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Version));
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode);
        var approved = (await approvedResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
            var tenantId = publicIds.Decode(PublicIdKind.Tenant, approved.TenantId);
            var organizationId = publicIds.Decode(PublicIdKind.Organization, approved.OrganizationId, tenantId);
            var branchId = publicIds.Decode(PublicIdKind.Branch, approved.BranchId, tenantId);
            var user = new ApplicationUser
            {
                UserName = "clinic.admin@example.invalid",
                Email = "clinic.admin@example.invalid",
                DisplayName = "Clinic Administrator",
                EmailConfirmed = true
            };
            Assert.True((await userManager.CreateAsync(user, "Test-Only-Password1!")).Succeeded);
            if (await roleManager.FindByNameAsync(BookDocRoleNames.ClinicAdministrator) is null)
            {
                var role = new ApplicationRole
                {
                    Name = BookDocRoleNames.ClinicAdministrator,
                    IsActive = true
                };
                Assert.True((await roleManager.CreateAsync(role)).Succeeded);
                Assert.True((await roleManager.AddClaimAsync(
                    role,
                    new Claim(BookDocClaimTypes.Permission, FoundationPermissions.BranchesView))).Succeeded);
            }
            Assert.True((await userManager.AddToRoleAsync(user, BookDocRoleNames.ClinicAdministrator)).Succeeded);
            dbContext.UserScopes.Add(ApplicationUserScope.Create(
                user.Id,
                tenantId,
                organizationId,
                branchId,
                true,
                DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync();
        }

        ClearDevelopmentHeaders(client);
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("clinic.admin@example.invalid", "Test-Only-Password1!"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var session = (await loginResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<AuthSessionResponse>>())!.Data;
        Assert.False(session.IsPlatformOperator);
        using (var scope = factory.Services.CreateScope())
        {
            var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
            var expectedTenantId = publicIds.Decode(PublicIdKind.Tenant, approved.TenantId);
            var actualTenantId = publicIds.Decode(PublicIdKind.Tenant, session.Scope!.TenantId);
            Assert.Equal(expectedTenantId, actualTenantId);
            Assert.Equal(
                publicIds.Decode(PublicIdKind.Organization, approved.OrganizationId, expectedTenantId),
                publicIds.Decode(PublicIdKind.Organization, session.Scope.OrganizationId, actualTenantId));
            Assert.Equal(
                publicIds.Decode(PublicIdKind.Branch, approved.BranchId, expectedTenantId),
                publicIds.Decode(PublicIdKind.Branch, session.Scope.BranchId!, actualTenantId));
        }
        Assert.Contains(FoundationPermissions.BranchesView, session.Permissions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var branchResponse = await client.GetAsync($"/api/v1/branches/{approved.BranchId}/configuration");
        Assert.Equal(HttpStatusCode.OK, branchResponse.StatusCode);
    }

    private static void ClearDevelopmentHeaders(HttpClient client)
    {
        foreach (var header in new[]
                 {
                     "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator"
                 })
        {
            client.DefaultRequestHeaders.Remove(header);
        }
    }
}
