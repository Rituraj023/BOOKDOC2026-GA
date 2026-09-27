using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Workforce;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Workforce;

namespace BookDoc2026.Application.Workforce;

public sealed class PractitionerService(
    IPractitionerRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlation) : IPractitionerEligibility
{
    public async Task<PractitionerResponse> CreateAsync(long branchId, CreatePractitionerRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PractitionersManage, branchId, cancellationToken);
        var stakeholderId = publicIds.Decode(PublicIdKind.Stakeholder, request.StakeholderId, branch.TenantId);
        var subjectId = publicIds.Decode(PublicIdKind.IdentitySubject, request.IdentitySubjectId);
        if (!await repository.PersonStakeholderExistsAsync(stakeholderId, cancellationToken))
            throw new NotFoundException("An active person stakeholder was not found.");
        if (!await repository.ActiveIdentitySubjectExistsAsync(subjectId, cancellationToken))
            throw new NotFoundException("An active identity subject was not found.");
        if (await repository.ProfileConflictExistsAsync(stakeholderId, subjectId,
                request.PractitionerCode.Trim().ToUpperInvariant(), cancellationToken))
            throw new DomainRuleException("Practitioner stakeholder, identity subject or code is already registered.");
        var now = clock.UtcNow;
        var profile = PractitionerProfile.Create(branch.TenantId, stakeholderId, subjectId,
            request.PractitionerCode, request.PractitionerTypeCode, now);
        await repository.AddProfileAsync(profile, Audit(branch, profile.Id, "Practitioner.Created",
            new { profile.StakeholderId, profile.IdentitySubjectId, profile.PractitionerCode }, now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new(profile, [], []));
    }

    public async Task<PractitionerResponse> GetAsync(long branchId, long practitionerId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.PractitionersView, branchId, cancellationToken);
        return Map(await repository.GetAsync(practitionerId, false, cancellationToken)
            ?? throw new NotFoundException("Practitioner was not found."));
    }

    public async Task<IReadOnlyCollection<PractitionerSummaryResponse>> ListBranchPractitionersAsync(
        long branchId, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PractitionersView, branchId, cancellationToken);
        var items = await repository.ListBranchPractitionersAsync(branchId, cancellationToken);
        return items.Select(item => new PractitionerSummaryResponse(
            publicIds.Encode(PublicIdKind.Practitioner, item.Profile.Id, item.Profile.TenantId),
            item.Profile.PractitionerCode,
            item.Profile.PractitionerTypeCode,
            item.DisplayName,
            item.Profile.Status.ToString(),
            item.Assignments.Select(a => a.RoleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            item.Assignments.Select(a => publicIds.Encode(PublicIdKind.ClinicalService, a.ServiceId, a.TenantId)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        )).ToArray();
    }

    public async Task<PractitionerResponse> AddCredentialAsync(long branchId, long practitionerId,
        AddPractitionerCredentialRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PractitionersManage, branchId, cancellationToken);
        var aggregate = await RequirePractitionerAsync(practitionerId, true, cancellationToken);
        var now = clock.UtcNow;
        var credential = PractitionerCredential.Create(branch.TenantId, practitionerId, request.CredentialTypeCode,
            request.RegistrationNumber, request.IssuingAuthority, request.ValidFrom, request.ValidTo, now);
        await repository.AddCredentialAsync(credential, Audit(branch, credential.Id, "PractitionerCredential.Added",
            new { credential.PractitionerId, credential.CredentialTypeCode, credential.ValidFrom, credential.ValidTo }, now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate with { Credentials = aggregate.Credentials.Append(credential).ToArray() });
    }

    public Task<PractitionerResponse> VerifyCredentialAsync(long branchId, long practitionerId, long credentialId,
        DecidePractitionerCredentialRequest request, CancellationToken cancellationToken) => DecideCredentialAsync(
            branchId, practitionerId, credentialId, request, true, cancellationToken);

    public Task<PractitionerResponse> RejectCredentialAsync(long branchId, long practitionerId, long credentialId,
        DecidePractitionerCredentialRequest request, CancellationToken cancellationToken) => DecideCredentialAsync(
            branchId, practitionerId, credentialId, request, false, cancellationToken);

    private async Task<PractitionerResponse> DecideCredentialAsync(long branchId, long practitionerId,
        long credentialId, DecidePractitionerCredentialRequest request, bool verify,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PractitionerCredentialsVerify, branchId,
            cancellationToken);
        var aggregate = await RequirePractitionerAsync(practitionerId, true, cancellationToken);
        var credential = await repository.GetCredentialAsync(practitionerId, credentialId, true, cancellationToken)
            ?? throw new NotFoundException("Practitioner credential was not found.");
        var now = clock.UtcNow;
        if (verify) credential.Verify(request.ExpectedVersion, actor.ActorId, now);
        else credential.Reject(request.ExpectedVersion, actor.ActorId, request.Reason ?? string.Empty, now);
        await repository.AddAuditAsync(Audit(branch, credential.Id,
            verify ? "PractitionerCredential.Verified" : "PractitionerCredential.Rejected",
            new { credential.PractitionerId, credential.CredentialTypeCode, credential.VerificationStatus }, now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    public async Task<PractitionerResponse> AddAssignmentAsync(long branchId, long practitionerId,
        AddPractitionerAssignmentRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PractitionerAssignmentsManage, branchId,
            cancellationToken);
        var aggregate = await RequirePractitionerAsync(practitionerId, true, cancellationToken);
        var serviceId = publicIds.Decode(PublicIdKind.ClinicalService, request.ServiceId, branch.TenantId);
        var resourceId = publicIds.DecodeOptional(PublicIdKind.BookableResource, request.BookableResourceId,
            branch.TenantId);
        if (!await repository.ServiceExistsAsync(serviceId, cancellationToken))
            throw new NotFoundException("Clinical service was not found.");
        if (resourceId.HasValue && !await repository.PractitionerResourceSupportsServiceAsync(branchId,
                resourceId.Value, serviceId, cancellationToken))
            throw new DomainRuleException("The selected resource must be an active practitioner resource with this service capability.");
        var now = clock.UtcNow;
        var assignment = PractitionerAssignment.Create(branch.TenantId, practitionerId, branchId, serviceId,
            resourceId, request.RoleCode, request.EffectiveFrom, request.EffectiveTo, now);
        if (aggregate.Assignments.Any(item => item.BranchId == branchId && item.ServiceId == serviceId
            && item.Status != PractitionerAssignmentStatus.Ended
            && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= assignment.EffectiveFrom)
            && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= item.EffectiveFrom)))
            throw new DomainRuleException("An overlapping practitioner assignment already exists for this branch and service.");
        await repository.AddAssignmentAsync(assignment, Audit(branch, assignment.Id, "PractitionerAssignment.Added",
            new { assignment.PractitionerId, assignment.ServiceId, assignment.BookableResourceId,
                assignment.EffectiveFrom, assignment.EffectiveTo }, now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate with { Assignments = aggregate.Assignments.Append(assignment).ToArray() });
    }

    public Task<PractitionerResponse> ActivateAsync(long branchId, long practitionerId,
        ChangePractitionerStatusRequest request, CancellationToken cancellationToken) => ChangeProfileAsync(branchId,
            practitionerId, request, "Practitioner.Activated", (aggregate, now, date) => aggregate.Profile.Activate(
                request.ExpectedVersion, aggregate.Credentials.Any(item => item.IsCurrent(date)), now), cancellationToken);

    public Task<PractitionerResponse> SuspendAsync(long branchId, long practitionerId,
        ChangePractitionerStatusRequest request, CancellationToken cancellationToken) => ChangeProfileAsync(branchId,
            practitionerId, request, "Practitioner.Suspended", (aggregate, now, _) =>
                aggregate.Profile.Suspend(request.ExpectedVersion, now), cancellationToken);

    public Task<PractitionerResponse> DeactivateAsync(long branchId, long practitionerId,
        ChangePractitionerStatusRequest request, CancellationToken cancellationToken) => ChangeProfileAsync(branchId,
            practitionerId, request, "Practitioner.Deactivated", (aggregate, now, _) =>
                aggregate.Profile.Deactivate(request.ExpectedVersion, now), cancellationToken);

    public Task<PractitionerResponse> SuspendAssignmentAsync(long branchId, long practitionerId, long assignmentId,
        ChangePractitionerStatusRequest request, CancellationToken cancellationToken) => ChangeAssignmentAsync(branchId,
            practitionerId, assignmentId, request, "PractitionerAssignment.Suspended",
            (assignment, now) => assignment.Suspend(request.ExpectedVersion, now), cancellationToken);

    public Task<PractitionerResponse> EndAssignmentAsync(long branchId, long practitionerId, long assignmentId,
        ChangePractitionerStatusRequest request, CancellationToken cancellationToken) => ChangeAssignmentAsync(branchId,
            practitionerId, assignmentId, request, "PractitionerAssignment.Ended",
            (assignment, now) => assignment.End(request.ExpectedVersion, now), cancellationToken);

    public async Task EnsureEligibleSignerAsync(long identitySubjectId, long branchId, long serviceId, DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!await repository.HasEligibleSignerAsync(identitySubjectId, branchId, serviceId, date, cancellationToken))
            throw new ForbiddenException("The actor is not an active, currently credentialed practitioner assigned to this branch and service.");
    }

    public async Task EnsureEligiblePerformerAsync(long identitySubjectId, long branchId, long serviceId,
        DateOnly date, CancellationToken cancellationToken)
    {
        if (!await repository.HasEligibleSignerAsync(identitySubjectId, branchId, serviceId, date,
                cancellationToken))
            throw new ForbiddenException(
                "The actor is not an active, currently credentialed practitioner assigned to this branch and service.");
    }

    private async Task<PractitionerResponse> ChangeProfileAsync(long branchId, long practitionerId,
        ChangePractitionerStatusRequest request, string action,
        Action<PractitionerAggregate, DateTimeOffset, DateOnly> transition, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PractitionersManage, branchId, cancellationToken);
        var aggregate = await RequirePractitionerAsync(practitionerId, true, cancellationToken);
        var now = clock.UtcNow;
        transition(aggregate, now, LocalDate(branch, now));
        await repository.AddAuditAsync(Audit(branch, practitionerId, action,
            new { aggregate.Profile.Status, aggregate.Profile.Version }, now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    private async Task<PractitionerResponse> ChangeAssignmentAsync(long branchId, long practitionerId,
        long assignmentId, ChangePractitionerStatusRequest request, string action,
        Action<PractitionerAssignment, DateTimeOffset> transition, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PractitionerAssignmentsManage, branchId,
            cancellationToken);
        var aggregate = await RequirePractitionerAsync(practitionerId, true, cancellationToken);
        var assignment = await repository.GetAssignmentAsync(practitionerId, assignmentId, true, cancellationToken)
            ?? throw new NotFoundException("Practitioner assignment was not found.");
        if (assignment.BranchId != branchId) throw new ForbiddenException("Assignment belongs to another branch.");
        var now = clock.UtcNow;
        transition(assignment, now);
        await repository.AddAuditAsync(Audit(branch, assignment.Id, action,
            new { assignment.PractitionerId, assignment.ServiceId, assignment.Status, assignment.Version }, now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    private async Task<PractitionerAggregate> RequirePractitionerAsync(long practitionerId, bool tracked,
        CancellationToken cancellationToken) => await repository.GetAsync(practitionerId, tracked, cancellationToken)
        ?? throw new NotFoundException("Practitioner was not found.");

    private async Task<Branch> RequireBranchAsync(string permission, long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this practitioner operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private AuditEvent Audit(Branch branch, long entityId, string action, object metadata, DateTimeOffset now) =>
        AuditEvent.Record(branch.TenantId, branch.Id, actor.ActorId, action, nameof(PractitionerProfile), entityId,
            JsonSerializer.Serialize(metadata), correlation.CorrelationId, now);

    private static DateOnly LocalDate(Branch branch, DateTimeOffset now) => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId)).DateTime);

    private PractitionerResponse Map(PractitionerAggregate aggregate) => new(
        publicIds.Encode(PublicIdKind.Practitioner, aggregate.Profile.Id, aggregate.Profile.TenantId),
        publicIds.Encode(PublicIdKind.Stakeholder, aggregate.Profile.StakeholderId, aggregate.Profile.TenantId),
        publicIds.Encode(PublicIdKind.IdentitySubject, aggregate.Profile.IdentitySubjectId),
        aggregate.Profile.PractitionerCode, aggregate.Profile.PractitionerTypeCode, aggregate.Profile.Status.ToString(),
        aggregate.Profile.Version,
        aggregate.Credentials.OrderBy(item => item.CreatedUtc).Select(item => new PractitionerCredentialResponse(
            publicIds.Encode(PublicIdKind.PractitionerCredential, item.Id, item.TenantId), item.CredentialTypeCode,
            item.RegistrationNumber, item.IssuingAuthority, item.ValidFrom, item.ValidTo,
            item.VerificationStatus.ToString(), item.VerifiedByActorId.HasValue
                ? publicIds.Encode(PublicIdKind.IdentitySubject, item.VerifiedByActorId.Value) : null,
            item.VerifiedUtc, item.DecisionReason, item.Version)).ToArray(),
        aggregate.Assignments.Where(item => actor.BranchIds.Contains(item.BranchId))
            .OrderBy(item => item.EffectiveFrom).Select(item => new PractitionerAssignmentResponse(
            publicIds.Encode(PublicIdKind.PractitionerAssignment, item.Id, item.TenantId),
            publicIds.Encode(PublicIdKind.Branch, item.BranchId, item.TenantId),
            publicIds.Encode(PublicIdKind.ClinicalService, item.ServiceId, item.TenantId),
            publicIds.EncodeOptional(PublicIdKind.BookableResource, item.BookableResourceId, item.TenantId),
            item.RoleCode, item.EffectiveFrom, item.EffectiveTo, item.Status.ToString(), item.Version)).ToArray());
}
