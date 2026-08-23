using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Workforce;

namespace BookDoc2026.Application.Abstractions;

public sealed record PractitionerAggregate(
    PractitionerProfile Profile,
    IReadOnlyCollection<PractitionerCredential> Credentials,
    IReadOnlyCollection<PractitionerAssignment> Assignments);

public interface IPractitionerRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<bool> PersonStakeholderExistsAsync(long stakeholderId, CancellationToken cancellationToken);
    Task<bool> ActiveIdentitySubjectExistsAsync(long subjectId, CancellationToken cancellationToken);
    Task<bool> ProfileConflictExistsAsync(long stakeholderId, long subjectId, string practitionerCode,
        CancellationToken cancellationToken);
    Task<bool> ServiceExistsAsync(long serviceId, CancellationToken cancellationToken);
    Task<bool> PractitionerResourceSupportsServiceAsync(long branchId, long resourceId, long serviceId,
        CancellationToken cancellationToken);
    Task<PractitionerAggregate?> GetAsync(long practitionerId, bool tracked, CancellationToken cancellationToken);
    Task<PractitionerCredential?> GetCredentialAsync(long practitionerId, long credentialId, bool tracked,
        CancellationToken cancellationToken);
    Task<PractitionerAssignment?> GetAssignmentAsync(long practitionerId, long assignmentId, bool tracked,
        CancellationToken cancellationToken);
    Task<bool> HasEligibleSignerAsync(long subjectId, long branchId, long serviceId, DateOnly date,
        CancellationToken cancellationToken);
    Task AddProfileAsync(PractitionerProfile profile, AuditEvent audit, CancellationToken cancellationToken);
    Task AddCredentialAsync(PractitionerCredential credential, AuditEvent audit, CancellationToken cancellationToken);
    Task AddAssignmentAsync(PractitionerAssignment assignment, AuditEvent audit, CancellationToken cancellationToken);
    Task AddAuditAsync(AuditEvent audit, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPractitionerEligibility
{
    Task EnsureEligibleSignerAsync(long identitySubjectId, long branchId, long serviceId, DateOnly date,
        CancellationToken cancellationToken);
}
