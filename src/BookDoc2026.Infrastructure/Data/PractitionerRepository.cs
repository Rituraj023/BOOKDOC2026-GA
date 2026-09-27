using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Domain.Stakeholders;
using BookDoc2026.Domain.Workforce;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class PractitionerRepository(BookDocDbContext db) : IPractitionerRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        db.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public async Task<IReadOnlyCollection<PractitionerSummaryItem>> ListBranchPractitionersAsync(
        long branchId, CancellationToken cancellationToken)
    {
        var branch = await db.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);
        if (branch is null) return [];

        var assignedPractitionerIds = await db.PractitionerAssignments
            .Where(a => a.TenantId == branch.TenantId && a.BranchId == branchId && a.Status == PractitionerAssignmentStatus.Active)
            .Select(a => a.PractitionerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = db.PractitionerProfiles
            .AsNoTracking()
            .Where(p => p.TenantId == branch.TenantId && p.Status == PractitionerStatus.Active);

        if (assignedPractitionerIds.Count > 0)
        {
            query = query.Where(p => assignedPractitionerIds.Contains(p.Id));
        }

        var profiles = await query.ToListAsync(cancellationToken);
        if (profiles.Count == 0) return [];

        var practitionerIds = profiles.Select(p => p.Id).ToList();
        var stakeholderIds = profiles.Select(p => p.StakeholderId).ToList();

        var assignments = await db.PractitionerAssignments
            .AsNoTracking()
            .Where(a => practitionerIds.Contains(a.PractitionerId) && a.Status == PractitionerAssignmentStatus.Active)
            .ToListAsync(cancellationToken);

        var persons = await db.StakeholderPersons
            .AsNoTracking()
            .Where(sp => stakeholderIds.Contains(sp.StakeholderId))
            .ToListAsync(cancellationToken);

        var personsByStakeholder = persons.ToDictionary(sp => sp.StakeholderId);

        return profiles.Select(p =>
        {
            var name = personsByStakeholder.TryGetValue(p.StakeholderId, out var person) && !string.IsNullOrWhiteSpace(person.DisplayName)
                ? person.DisplayName
                : p.PractitionerCode;
            return new PractitionerSummaryItem(
                p,
                name,
                assignments.Where(a => a.PractitionerId == p.Id).ToArray());
        }).ToArray();
    }

    public async Task<bool> PersonStakeholderExistsAsync(long stakeholderId, CancellationToken cancellationToken)
    {
        if (!await db.Stakeholders.AnyAsync(item => item.Id == stakeholderId && item.Type == StakeholderType.Person
                && item.Status == StakeholderStatus.Active, cancellationToken)) return false;
        return await db.StakeholderPersons.AnyAsync(item => item.StakeholderId == stakeholderId, cancellationToken);
    }

    public Task<bool> ActiveIdentitySubjectExistsAsync(long subjectId, CancellationToken cancellationToken) =>
        subjectId is <= 0 or > uint.MaxValue
            ? Task.FromResult(false)
            : db.Users.IgnoreQueryFilters().AnyAsync(item => item.Id == (uint)subjectId && item.IsActive,
                cancellationToken);

    public Task<bool> ProfileConflictExistsAsync(long stakeholderId, long subjectId, string practitionerCode,
        CancellationToken cancellationToken) => db.PractitionerProfiles.AnyAsync(item =>
        item.StakeholderId == stakeholderId || item.IdentitySubjectId == (uint)subjectId
        || item.PractitionerCode == practitionerCode, cancellationToken);

    public Task<bool> ServiceExistsAsync(long serviceId, CancellationToken cancellationToken) =>
        db.ClinicalServices.AnyAsync(item => item.Id == serviceId && item.Status == CatalogItemStatus.Active,
            cancellationToken);

    public Task<bool> PractitionerResourceSupportsServiceAsync(long branchId, long resourceId, long serviceId,
        CancellationToken cancellationToken) => db.BookableResources.AnyAsync(resource =>
        resource.Id == resourceId && resource.BranchId == branchId && resource.IsActive
        && db.ResourceCategories.Any(category => category.Id == resource.CategoryId && category.IsActive
            && category.Kind == ResourceKind.Practitioner)
        && db.ResourceCapabilities.Any(capability => capability.ResourceId == resourceId
            && capability.ServiceId == serviceId && capability.IsActive), cancellationToken);

    public async Task<PractitionerAggregate?> GetAsync(long practitionerId, bool tracked,
        CancellationToken cancellationToken)
    {
        var profiles = db.PractitionerProfiles.Where(item => item.Id == practitionerId);
        var profile = await (tracked ? profiles : profiles.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        if (profile is null) return null;
        var credentials = db.PractitionerCredentials.Where(item => item.PractitionerId == practitionerId);
        var assignments = db.PractitionerAssignments.Where(item => item.PractitionerId == practitionerId);
        return new PractitionerAggregate(profile,
            await (tracked ? credentials : credentials.AsNoTracking()).ToArrayAsync(cancellationToken),
            await (tracked ? assignments : assignments.AsNoTracking()).ToArrayAsync(cancellationToken));
    }

    public Task<PractitionerCredential?> GetCredentialAsync(long practitionerId, long credentialId, bool tracked,
        CancellationToken cancellationToken)
    {
        var query = db.PractitionerCredentials.Where(item => item.PractitionerId == practitionerId
            && item.Id == credentialId);
        return (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<PractitionerAssignment?> GetAssignmentAsync(long practitionerId, long assignmentId, bool tracked,
        CancellationToken cancellationToken)
    {
        var query = db.PractitionerAssignments.Where(item => item.PractitionerId == practitionerId
            && item.Id == assignmentId);
        return (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasEligibleSignerAsync(long subjectId, long branchId, long serviceId, DateOnly date,
        CancellationToken cancellationToken) => db.PractitionerProfiles.AnyAsync(profile =>
        subjectId > 0 && subjectId <= uint.MaxValue && profile.IdentitySubjectId == (uint)subjectId
        && profile.Status == PractitionerStatus.Active
        && db.Users.Any(user => user.Id == profile.IdentitySubjectId && user.IsActive)
        && db.Stakeholders.Any(stakeholder => stakeholder.Id == profile.StakeholderId
            && stakeholder.Status == StakeholderStatus.Active)
        && db.PractitionerCredentials.Any(credential => credential.PractitionerId == profile.Id
            && credential.VerificationStatus == CredentialVerificationStatus.Verified
            && credential.ValidFrom <= date && (!credential.ValidTo.HasValue || credential.ValidTo.Value >= date))
        && db.PractitionerAssignments.Any(assignment => assignment.PractitionerId == profile.Id
            && assignment.BranchId == branchId && assignment.ServiceId == serviceId
            && assignment.Status == PractitionerAssignmentStatus.Active
            && assignment.EffectiveFrom <= date
            && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= date)), cancellationToken);

    public async Task AddProfileAsync(PractitionerProfile profile, AuditEvent audit,
        CancellationToken cancellationToken)
    {
        await db.PractitionerProfiles.AddAsync(profile, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public async Task AddCredentialAsync(PractitionerCredential credential, AuditEvent audit,
        CancellationToken cancellationToken)
    {
        await db.PractitionerCredentials.AddAsync(credential, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public async Task AddAssignmentAsync(PractitionerAssignment assignment, AuditEvent audit,
        CancellationToken cancellationToken)
    {
        await db.PractitionerAssignments.AddAsync(assignment, cancellationToken);
        await db.AuditEvents.AddAsync(audit, cancellationToken);
    }

    public Task AddAuditAsync(AuditEvent audit, CancellationToken cancellationToken) =>
        db.AuditEvents.AddAsync(audit, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
