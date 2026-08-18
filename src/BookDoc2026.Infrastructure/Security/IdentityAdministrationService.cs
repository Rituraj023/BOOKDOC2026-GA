using System.Security.Claims;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Security;

public sealed class IdentityAdministrationService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    BookDocDbContext dbContext,
    IPublicIdCodec publicIds,
    ICurrentActor actor,
    IClock clock) : IIdentityAdministrationService
{
    public async Task<IdentityUserResponse> CreateUserAsync(
        CreateIdentityUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.IsPlatformOperator || !actor.HasPermission(FoundationPermissions.UsersManage))
        {
            throw new ForbiddenException("Platform user management permission is required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new DomainRuleException("An identity user already uses this email address.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = Required(request.DisplayName, 160),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            EmailConfirmed = false,
            IsActive = true,
            CreatedUtc = clock.UtcNow,
            ModifiedUtc = clock.UtcNow
        };
        EnsureSuccess(await userManager.CreateAsync(user, request.TemporaryPassword));
        return Map(user);
    }

    public async Task<ClinicAdministratorAssignment> AssignClinicAdministratorAsync(
        long subjectId,
        long tenantId,
        long organizationId,
        long branchId,
        CancellationToken cancellationToken)
    {
        if (subjectId is <= 0 or > uint.MaxValue)
        {
            throw new NotFoundException("The identity user was not found.");
        }

        var user = await userManager.FindByIdAsync(((uint)subjectId).ToString());
        if (user is null || !user.IsActive)
        {
            throw new NotFoundException("The active identity user was not found.");
        }

        var existing = await dbContext.UserScopes.SingleOrDefaultAsync(
            scope => scope.UserId == user.Id
                && scope.TenantId == tenantId
                && scope.OrganizationId == organizationId
                && scope.BranchId == branchId,
            cancellationToken);
        if (existing is not null)
        {
            throw new DomainRuleException("This user already has access to the branch.");
        }

        var role = await roleManager.FindByNameAsync(BookDocRoleNames.ClinicAdministrator)
            ?? throw new InvalidOperationException("The ClinicAdministrator role has not been provisioned.");
        if (!await userManager.IsInRoleAsync(user, BookDocRoleNames.ClinicAdministrator))
        {
            EnsureSuccess(await userManager.AddToRoleAsync(user, BookDocRoleNames.ClinicAdministrator));
        }

        var hasDefault = await dbContext.UserScopes.AnyAsync(
            scope => scope.UserId == user.Id && scope.IsDefault,
            cancellationToken);
        var scope = ApplicationUserScope.Create(
            user.Id,
            tenantId,
            organizationId,
            branchId,
            !hasDefault,
            clock.UtcNow);
        dbContext.UserScopes.Add(scope);
        await dbContext.SaveChangesAsync(cancellationToken);
        var permissions = (await roleManager.GetClaimsAsync(role))
            .Where(claim => claim.Type == BookDocClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new ClinicAdministratorAssignment(
            scope.Id,
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            permissions);
    }

    private IdentityUserResponse Map(ApplicationUser user) => new(
        publicIds.Encode(PublicIdKind.IdentitySubject, user.Id),
        user.DisplayName,
        user.Email ?? string.Empty,
        user.PhoneNumber,
        user.IsActive);

    private static string Required(string value, int maxLength)
    {
        var result = value.Trim();
        if (string.IsNullOrWhiteSpace(result) || result.Length > maxLength)
        {
            throw new DomainRuleException($"Display name is required and must not exceed {maxLength} characters.");
        }
        return result;
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new DomainRuleException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
