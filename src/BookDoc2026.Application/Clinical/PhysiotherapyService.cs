using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Clinical;

public sealed class PhysiotherapyService(
    IPhysiotherapyRepository repository,
    IEncounterRepository encounters,
    IPractitionerEligibility practitionerEligibility,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlation)
{
    public async Task<PhysiotherapyCarePlanResponse> CreateAsync(long branchId,
        CreatePhysiotherapyCarePlanRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PhysiotherapyCarePlansManage, branchId,
            cancellationToken);
        var encounterId = publicIds.Decode(PublicIdKind.Encounter, request.InitialEncounterId, branch.TenantId);
        var encounter = await encounters.GetEncounterAsync(branchId, encounterId, false, cancellationToken)
            ?? throw new NotFoundException("Initial encounter was not found.");
        if (await repository.CarePlanExistsForEncounterAsync(branchId, encounterId, cancellationToken))
            throw new DomainRuleException("A Physiotherapy care plan already exists for this initial encounter.");
        var now = clock.UtcNow;
        await EnsureEligibleAsync(branch, encounter.Encounter.ServiceId, now, cancellationToken);
        var (plan, revision) = PhysiotherapyCarePlan.Start(encounter.Encounter, encounter.Latest,
            Map(request.Content), actor.ActorId, now);
        await repository.AddCarePlanAsync(plan, revision,
            Audit(branch, plan.Id, "PhysiotherapyCarePlan.Created", new
            { InitialEncounterId = encounterId, RevisionId = revision.Id, revision.ContentHash }, now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new PhysiotherapyCarePlanAggregate(plan, [revision], [], []));
    }

    public async Task<PhysiotherapyCarePlanResponse> GetAsync(long branchId, long carePlanId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.PhysiotherapyCarePlansView, branchId,
            cancellationToken);
        return Map(await RequirePlanAsync(branchId, carePlanId, false, cancellationToken));
    }

    public async Task<IReadOnlyCollection<PhysiotherapyCarePlanListItemResponse>> ListAsync(long branchId,
        int take, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PhysiotherapyCarePlansView, branchId,
            cancellationToken);
        var limited = Math.Clamp(take, 1, 100);
        return (await repository.ListCarePlansAsync(branch.Id, limited, cancellationToken)).Select(item => new
            PhysiotherapyCarePlanListItemResponse(
                publicIds.Encode(PublicIdKind.PhysiotherapyCarePlan, item.Plan.Id, branch.TenantId),
                item.Plan.CarePlanNumber,
                publicIds.Encode(PublicIdKind.Patient, item.Plan.PatientId, branch.TenantId),
                item.PatientNumber,
                item.PatientDisplayName,
                publicIds.Encode(PublicIdKind.ClinicalService, item.Plan.ServiceId, branch.TenantId),
                item.Plan.Status.ToString(),
                item.Plan.LatestRevisionNumber,
                item.ReviewOn,
                item.SessionCount,
                item.LastSessionUtc,
                item.Plan.Version)).ToArray();
    }

    public async Task<PhysiotherapyCarePlanResponse> ReviseAsync(long branchId, long carePlanId,
        RevisePhysiotherapyCarePlanRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PhysiotherapyCarePlansManage, branchId,
            cancellationToken);
        var aggregate = await RequirePlanAsync(branchId, carePlanId, true, cancellationToken);
        var now = clock.UtcNow;
        await EnsureEligibleAsync(branch, aggregate.Plan.ServiceId, now, cancellationToken);
        var revision = aggregate.Plan.Revise(aggregate.LatestRevision, Map(request.Content), request.Reason,
            request.ExpectedVersion, actor.ActorId, now);
        await repository.AddRevisionAsync(revision,
            Audit(branch, carePlanId, "PhysiotherapyCarePlan.Revised", new
            { RevisionId = revision.Id, revision.RevisionNumber, revision.ContentHash,
                HasReason = revision.ChangeReason is not null }, now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate with { Revisions = aggregate.Revisions.Append(revision).ToArray() });
    }

    public Task<PhysiotherapyCarePlanResponse> ActivateAsync(long branchId, long carePlanId,
        long expectedVersion, CancellationToken cancellationToken) => ChangeStatusAsync(branchId, carePlanId,
        expectedVersion, "PhysiotherapyCarePlan.Activated", (plan, now) =>
            plan.Activate(expectedVersion, actor.ActorId, now), cancellationToken);

    public Task<PhysiotherapyCarePlanResponse> CloseAsync(long branchId, long carePlanId,
        ChangePhysiotherapyCarePlanStatusRequest request, bool discontinued,
        CancellationToken cancellationToken) => ChangeStatusAsync(branchId, carePlanId, request.ExpectedVersion,
        discontinued ? "PhysiotherapyCarePlan.Discontinued" : "PhysiotherapyCarePlan.Completed",
        (plan, now) => plan.Close(discontinued, request.Reason, request.ExpectedVersion, actor.ActorId, now),
        cancellationToken);

    public async Task<PhysiotherapyCarePlanResponse> RecordSessionAsync(long branchId, long carePlanId,
        RecordPhysiotherapySessionRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PhysiotherapySessionsRecord, branchId,
            cancellationToken);
        var aggregate = await RequirePlanAsync(branchId, carePlanId, false, cancellationToken);
        var encounterId = publicIds.Decode(PublicIdKind.Encounter, request.EncounterId, branch.TenantId);
        var encounter = await encounters.GetEncounterAsync(branchId, encounterId, false, cancellationToken)
            ?? throw new NotFoundException("Treatment encounter was not found.");
        if (await repository.SessionExistsForEncounterAsync(branchId, encounterId, cancellationToken))
            throw new DomainRuleException("A Physiotherapy treatment session already exists for this encounter.");
        var now = clock.UtcNow;
        await EnsureEligibleAsync(branch, aggregate.Plan.ServiceId, now, cancellationToken);
        var content = PhysiotherapySessionContent.Create(request.SubjectiveResponse, request.Interventions,
            request.Tolerance, request.NextPlan, request.HadAdverseEvent, request.AdverseEventDetails);
        var session = PhysiotherapyTreatmentSession.Record(aggregate.Plan, encounter.Encounter, encounter.Latest,
            aggregate.Sessions.Count + 1, content, actor.ActorId, now);
        await repository.AddSessionAsync(session,
            Audit(branch, carePlanId, "PhysiotherapySession.Recorded", new
            { SessionId = session.Id, session.EncounterId, session.SequenceNumber, session.ContentHash,
                session.HadAdverseEvent }, now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate with { Sessions = aggregate.Sessions.Append(session).ToArray() });
    }

    public async Task<PhysiotherapyCarePlanResponse> RecordOutcomeAsync(long branchId, long carePlanId,
        RecordPhysiotherapyOutcomeRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PhysiotherapyOutcomesRecord, branchId,
            cancellationToken);
        var aggregate = await RequirePlanAsync(branchId, carePlanId, false, cancellationToken);
        PhysiotherapyTreatmentSession? session = null;
        if (!string.IsNullOrWhiteSpace(request.TreatmentSessionId))
        {
            var sessionId = publicIds.Decode(PublicIdKind.PhysiotherapyTreatmentSession,
                request.TreatmentSessionId, branch.TenantId);
            session = aggregate.Sessions.SingleOrDefault(item => item.Id == sessionId)
                ?? throw new NotFoundException("Treatment session was not found in this care plan.");
        }
        var now = clock.UtcNow;
        await EnsureEligibleAsync(branch, aggregate.Plan.ServiceId, now, cancellationToken);
        var outcome = PhysiotherapyOutcomeObservation.Record(aggregate.Plan, session, request.ContextCode,
            request.MeasureCode, request.ToolVersion, request.Value, request.Unit, request.BodySite,
            request.LateralityCode, request.ObservedUtc, actor.ActorId, now);
        await repository.AddOutcomeAsync(outcome,
            Audit(branch, carePlanId, "PhysiotherapyOutcome.Recorded", new
            { OutcomeId = outcome.Id, outcome.TreatmentSessionId, outcome.ContextCode,
                outcome.MeasureCode, outcome.ToolVersion }, now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate with { Outcomes = aggregate.Outcomes.Append(outcome).ToArray() });
    }

    private async Task<PhysiotherapyCarePlanResponse> ChangeStatusAsync(long branchId, long carePlanId,
        long expectedVersion, string action, Action<PhysiotherapyCarePlan, DateTimeOffset> transition,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.PhysiotherapyCarePlansManage, branchId,
            cancellationToken);
        var aggregate = await RequirePlanAsync(branchId, carePlanId, true, cancellationToken);
        var now = clock.UtcNow;
        await EnsureEligibleAsync(branch, aggregate.Plan.ServiceId, now, cancellationToken);
        transition(aggregate.Plan, now);
        await repository.AddAuditAsync(Audit(branch, carePlanId, action,
            new { aggregate.Plan.Status, aggregate.Plan.Version, ExpectedVersion = expectedVersion }, now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    private async Task<PhysiotherapyCarePlanAggregate> RequirePlanAsync(long branchId, long carePlanId,
        bool tracked, CancellationToken cancellationToken) =>
        await repository.GetCarePlanAsync(branchId, carePlanId, tracked, cancellationToken)
        ?? throw new NotFoundException("Physiotherapy care plan was not found.");

    private async Task<Branch> RequireBranchAsync(string permission, long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this Physiotherapy operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private Task EnsureEligibleAsync(Branch branch, long serviceId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,
            TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId)).DateTime);
        return practitionerEligibility.EnsureEligibleSignerAsync(actor.ActorId, branch.Id, serviceId, localDate,
            cancellationToken);
    }

    private AuditEvent Audit(Branch branch, long carePlanId, string action, object metadata, DateTimeOffset now) =>
        AuditEvent.Record(branch.TenantId, branch.Id, actor.ActorId, action, nameof(PhysiotherapyCarePlan),
            carePlanId, JsonSerializer.Serialize(metadata), correlation.CorrelationId, now);

    private static PhysiotherapyCarePlanContent Map(PhysiotherapyCarePlanContentRequest request) =>
        PhysiotherapyCarePlanContent.Create(request.GoalSummary, request.FrequencyAndDuration,
            request.PlannedInterventions, request.Precautions, request.ReviewOn);

    private PhysiotherapyCarePlanResponse Map(PhysiotherapyCarePlanAggregate aggregate)
    {
        var tenantId = aggregate.Plan.TenantId;
        return new(
            publicIds.Encode(PublicIdKind.PhysiotherapyCarePlan, aggregate.Plan.Id, tenantId),
            publicIds.Encode(PublicIdKind.Patient, aggregate.Plan.PatientId, tenantId),
            publicIds.Encode(PublicIdKind.ClinicalService, aggregate.Plan.ServiceId, tenantId),
            publicIds.Encode(PublicIdKind.Encounter, aggregate.Plan.InitialEncounterId, tenantId),
            aggregate.Plan.CarePlanNumber, aggregate.Plan.Status.ToString(), aggregate.Plan.LatestRevisionNumber,
            aggregate.Plan.ActivatedUtc, Actor(aggregate.Plan.ActivatedByActorId), aggregate.Plan.ClosedUtc,
            Actor(aggregate.Plan.ClosedByActorId), aggregate.Plan.ClosureReason, aggregate.Plan.Version,
            aggregate.Revisions.OrderBy(item => item.RevisionNumber).Select(item => new PhysiotherapyCarePlanRevisionResponse(
                publicIds.Encode(PublicIdKind.PhysiotherapyCarePlanRevision, item.Id, tenantId),
                item.RevisionNumber, publicIds.EncodeOptional(PublicIdKind.PhysiotherapyCarePlanRevision,
                    item.ParentRevisionId, tenantId), Actor(item.AuthorActorId)!,
                new(item.GoalSummary, item.FrequencyAndDuration, item.PlannedInterventions, item.Precautions,
                    item.ReviewOn), item.ContentHash, item.ChangeReason, item.CreatedUtc)).ToArray(),
            aggregate.Sessions.OrderBy(item => item.SequenceNumber).Select(item => new PhysiotherapySessionResponse(
                publicIds.Encode(PublicIdKind.PhysiotherapyTreatmentSession, item.Id, tenantId),
                publicIds.Encode(PublicIdKind.Encounter, item.EncounterId, tenantId),
                publicIds.Encode(PublicIdKind.Booking, item.BookingId, tenantId), item.SequenceNumber,
                Actor(item.AuthorActorId)!, item.SubjectiveResponse, item.Interventions, item.Tolerance,
                item.NextPlan, item.HadAdverseEvent, item.AdverseEventDetails, item.ContentHash,
                item.PerformedUtc)).ToArray(),
            aggregate.Outcomes.OrderBy(item => item.ObservedUtc).Select(item => new PhysiotherapyOutcomeResponse(
                publicIds.Encode(PublicIdKind.PhysiotherapyOutcomeObservation, item.Id, tenantId),
                publicIds.EncodeOptional(PublicIdKind.PhysiotherapyTreatmentSession, item.TreatmentSessionId,
                    tenantId), item.ContextCode, item.MeasureCode, item.ToolVersion, item.Value, item.Unit,
                item.BodySite, item.LateralityCode, item.ObservedUtc, Actor(item.AuthorActorId)!)).ToArray());
    }

    private string? Actor(long? actorId) => actorId.HasValue
        ? publicIds.Encode(PublicIdKind.IdentitySubject, actorId.Value) : null;
}
