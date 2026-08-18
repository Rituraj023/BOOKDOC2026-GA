using BookDoc2026.Contracts.Auth;

namespace BookDoc2026.Application.Abstractions;

public sealed record ClinicAdministratorAssignment(
    long ScopeId,
    long SubjectId,
    string DisplayName,
    string Email,
    IReadOnlyCollection<string> Permissions);

public interface IIdentityAdministrationService
{
    Task<IdentityUserResponse> CreateUserAsync(
        CreateIdentityUserRequest request,
        CancellationToken cancellationToken);

    Task<ClinicAdministratorAssignment> AssignClinicAdministratorAsync(
        long subjectId,
        long tenantId,
        long organizationId,
        long branchId,
        CancellationToken cancellationToken);
}
