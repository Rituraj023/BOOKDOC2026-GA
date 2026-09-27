using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Clinical;

public sealed class EncounterService(
    IEncounterRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlation,
    IPractitionerEligibility practitionerEligibility)
{
    public async Task<EncounterResponse> StartAsync(long branchId, StartEncounterRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.EncounterDraftsManage, branchId, cancellationToken);
        var bookingId = publicIds.Decode(PublicIdKind.Booking, request.BookingId, branch.TenantId);
        var booking = await repository.GetBookingAsync(branchId, bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking was not found.");
        if (await repository.EncounterExistsForBookingAsync(branchId, bookingId, cancellationToken))
            throw new DomainRuleException("An encounter already exists for this booking.");
        var now = clock.UtcNow;
        var supervising = publicIds.DecodeOptional(PublicIdKind.Practitioner, request.SupervisingPractitionerId, branch.TenantId);
        var (encounter, revision) = ClinicalEncounter.Start(booking, Map(request.Content), actor.ActorId, supervising, now);
        var audit = Audit(branch, encounter, revision, "Encounter.Started", now);
        await repository.AddEncounterAsync(encounter, revision, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new EncounterAggregate(encounter, [revision]));
    }

    public async Task<EncounterResponse> GetAsync(long branchId, long encounterId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.EncountersView, branchId, cancellationToken);
        return Map(await repository.GetEncounterAsync(branchId, encounterId, false, cancellationToken)
            ?? throw new NotFoundException("Encounter was not found."));
    }

    public async Task<IReadOnlyCollection<ClinicalAgendaItemResponse>> ListAgendaAsync(long branchId,
        DateOnly localDate, int take, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.EncountersView, branchId, cancellationToken);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId);
        var startLocal = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = startLocal.AddDays(1);
        var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startLocal, zone), TimeSpan.Zero);
        var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocal, zone), TimeSpan.Zero);
        var limited = Math.Clamp(take, 1, 100);
        return (await repository.ListAgendaAsync(branchId, actor.ActorId, localDate, startUtc, endUtc, limited,
            cancellationToken)).Select(item => Map(item, branch.TenantId)).ToArray();
    }

    public Task<EncounterResponse> ReviseDraftAsync(long branchId, long encounterId,
        ReviseEncounterDraftRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(branchId, encounterId, FoundationPermissions.EncounterDraftsManage, "Encounter.DraftRevised",
            (aggregate, now) =>
            {
                if (!string.IsNullOrWhiteSpace(request.SupervisingPractitionerId))
                {
                    var supervising = publicIds.Decode(PublicIdKind.Practitioner, request.SupervisingPractitionerId, aggregate.Encounter.TenantId);
                    aggregate.Encounter.AssignSupervisingPractitioner(supervising, now);
                }
                return aggregate.Encounter.ReviseDraft(aggregate.Latest, Map(request.Content),
                    request.ExpectedVersion, actor.ActorId, now);
            }, false, cancellationToken);

    public Task<EncounterResponse> SignAsync(long branchId, long encounterId, SignEncounterRequest request,
        CancellationToken cancellationToken) =>
        ChangeAsync(branchId, encounterId, FoundationPermissions.EncountersSign, "Encounter.Signed",
            (aggregate, now) => aggregate.Encounter.Sign(aggregate.Latest, request.ExpectedVersion,
                actor.ActorId, now), true, cancellationToken);

    public Task<EncounterResponse> AmendAsync(long branchId, long encounterId, AmendEncounterRequest request,
        CancellationToken cancellationToken) =>
        ChangeAsync(branchId, encounterId, FoundationPermissions.EncountersAmend, "Encounter.Amended",
            (aggregate, now) => aggregate.Encounter.Amend(aggregate.Latest, Map(request.Content), request.Reason,
                request.ExpectedVersion, actor.ActorId, now), true, cancellationToken);

    private async Task<EncounterResponse> ChangeAsync(long branchId, long encounterId, string permission,
        string action, Func<EncounterAggregate, DateTimeOffset, EncounterRevision> transition,
        bool requiresEligiblePractitioner, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(permission, branchId, cancellationToken);
        var aggregate = await repository.GetEncounterAsync(branchId, encounterId, true, cancellationToken)
            ?? throw new NotFoundException("Encounter was not found.");
        var now = clock.UtcNow;
        if (requiresEligiblePractitioner)
        {
            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,
                TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId)).DateTime);
            await practitionerEligibility.EnsureEligibleSignerAsync(actor.ActorId, branchId,
                aggregate.Encounter.ServiceId, localDate, cancellationToken);
        }
        var revision = transition(aggregate, now);
        await repository.AddRevisionAsync(revision, Audit(branch, aggregate.Encounter, revision, action, now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new EncounterAggregate(aggregate.Encounter, aggregate.Revisions.Append(revision).ToArray()));
    }

    private async Task<Branch> RequireBranchAsync(string permission, long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this clinical operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private AuditEvent Audit(Branch branch, ClinicalEncounter encounter, EncounterRevision revision,
        string action, DateTimeOffset now) => AuditEvent.Record(branch.TenantId, branch.Id, actor.ActorId, action,
        nameof(ClinicalEncounter), encounter.Id,
        JsonSerializer.Serialize(new
        {
            RevisionId = revision.Id, revision.RevisionNumber, Kind = revision.Kind.ToString(), revision.ContentHash
        }), correlation.CorrelationId, now);

    private static EncounterContent Map(EncounterContentRequest request) => EncounterContent.Create(
        request.SpecialtyCode, request.TemplateKey, request.TemplateVersion, request.ChiefComplaint,
        request.History, request.Examination, request.Assessment, request.Plan, request.Instructions,
        request.BodySite, request.LateralityCode);

    private EncounterResponse Map(EncounterAggregate aggregate) => new(
        publicIds.Encode(PublicIdKind.Encounter, aggregate.Encounter.Id, aggregate.Encounter.TenantId),
        publicIds.Encode(PublicIdKind.Booking, aggregate.Encounter.BookingId, aggregate.Encounter.TenantId),
        publicIds.Encode(PublicIdKind.Patient, aggregate.Encounter.PatientId, aggregate.Encounter.TenantId),
        publicIds.Encode(PublicIdKind.ClinicalService, aggregate.Encounter.ServiceId, aggregate.Encounter.TenantId),
        aggregate.Encounter.EncounterNumber, aggregate.Encounter.Status.ToString(),
        aggregate.Encounter.LatestRevisionNumber, aggregate.Encounter.SignedUtc,
        aggregate.Encounter.SignedByActorId.HasValue
            ? publicIds.Encode(PublicIdKind.IdentitySubject, aggregate.Encounter.SignedByActorId.Value)
            : null,
        publicIds.EncodeOptional(PublicIdKind.Practitioner, aggregate.Encounter.SupervisingPractitionerId, aggregate.Encounter.TenantId),
        aggregate.Encounter.Version,
        aggregate.Revisions.OrderBy(item => item.RevisionNumber).Select(item => Map(item, aggregate.Encounter.TenantId)).ToArray());

    private EncounterRevisionResponse Map(EncounterRevision revision, long tenantId) => new(
        publicIds.Encode(PublicIdKind.EncounterRevision, revision.Id, tenantId), revision.RevisionNumber,
        revision.Kind.ToString(), publicIds.EncodeOptional(PublicIdKind.EncounterRevision, revision.ParentRevisionId, tenantId),
        publicIds.Encode(PublicIdKind.IdentitySubject, revision.AuthorActorId),
        new EncounterContentRequest(revision.SpecialtyCode, revision.TemplateKey, revision.TemplateVersion,
            revision.ChiefComplaint, revision.History, revision.Examination, revision.Assessment, revision.Plan,
            revision.Instructions, revision.BodySite, revision.LateralityCode),
        revision.ContentHash, revision.AmendmentReason, revision.SignedUtc, revision.CreatedUtc);

    private ClinicalAgendaItemResponse Map(ClinicalAgendaWorkItem item, long tenantId) => new(
        publicIds.Encode(PublicIdKind.Booking, item.Booking.Id, tenantId), item.Booking.BookingNumber,
        publicIds.Encode(PublicIdKind.Patient, item.Booking.PatientId, tenantId), item.PatientNumber,
        item.PatientDisplayName, publicIds.Encode(PublicIdKind.ClinicalService, item.Booking.ServiceId, tenantId),
        item.ServiceCode, item.ServiceName, item.Booking.StartUtc, item.Booking.EndUtc,
        item.Encounter is null ? null : publicIds.Encode(PublicIdKind.Encounter, item.Encounter.Id, tenantId),
        item.Encounter?.EncounterNumber, item.Encounter?.Status.ToString(), item.Encounter?.Version,
        item.CarePlan is null ? null : publicIds.Encode(PublicIdKind.PhysiotherapyCarePlan, item.CarePlan.Id, tenantId),
        item.CarePlan?.CarePlanNumber, item.CarePlan?.Status.ToString());
}
