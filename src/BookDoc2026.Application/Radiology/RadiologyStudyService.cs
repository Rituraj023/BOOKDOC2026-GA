using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Radiology;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Radiology;

namespace BookDoc2026.Application.Radiology;

public sealed class RadiologyStudyService(
    IRadiologyStudyRepository repository,
    IPractitionerEligibility practitionerEligibility,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlation)
{
    private const string RegisteredAction = "REGISTERED";
    private const string StartedAction = "STARTED";
    private const string AcquisitionRecordedAction = "ACQUISITION_RECORDED";
    private const string QualityAcceptedAction = "QUALITY_ACCEPTED";
    private const string RepeatRequiredAction = "REPEAT_REQUIRED";

    public async Task<RadiologyStudyResponse> GetByOrderAsync(long branchId, long orderId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.RadiologyStudiesView, branchId, cancellationToken);
        return Map(await repository.GetByOrderAsync(branchId, orderId, false, cancellationToken)
            ?? throw new NotFoundException("Radiology study was not found."), false);
    }

    public async Task<RadiologyStudyResponse> GetAsync(long branchId, long studyId,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.RadiologyStudiesView, branchId, cancellationToken);
        return Map(await repository.GetByIdAsync(branchId, studyId, false, cancellationToken)
            ?? throw new NotFoundException("Radiology study was not found."), false);
    }

    public async Task<IReadOnlyCollection<RadiologyEquipmentOptionResponse>> ListEligibleEquipmentAsync(
        long branchId, long studyId, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.RadiologyAcquisitionsRecord, branchId,
            cancellationToken);
        var aggregate = await RequireStudyAsync(branchId, studyId, false, cancellationToken);
        await EnsureEligibleAsync(branch, aggregate.Study.ServiceId, cancellationToken);
        var orderContext = await repository.GetOrderContextAsync(branchId, aggregate.Study.OrderId,
            cancellationToken) ?? throw new NotFoundException("Queued investigation order was not found.");
        var resources = await repository.ListEligibleEquipmentAsync(branchId, aggregate.Study.ServiceId,
            aggregate.Study.Modality, cancellationToken);
        if (orderContext.ServicePoint.ResourceId.HasValue)
            resources = resources.Where(item => item.Id == orderContext.ServicePoint.ResourceId.Value).ToArray();
        return resources.Select(item => new RadiologyEquipmentOptionResponse(
            publicIds.Encode(PublicIdKind.BookableResource, item.Id, item.TenantId), item.Code, item.Name)).ToArray();
    }

    public async Task<RadiologyStudyResponse> RegisterAsync(long branchId, long orderId,
        RegisterRadiologyStudyRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.RadiologyStudiesStart, branchId,
            cancellationToken);
        var fingerprint = Fingerprint(new { Action = RegisteredAction, OrderId = orderId });
        var replay = await FindReplayAsync(branchId, orderId, null, request.RequestId, RegisteredAction,
            fingerprint, cancellationToken);
        if (replay is not null) return replay;

        var context = await repository.GetOrderContextAsync(branchId, orderId, cancellationToken)
            ?? throw new NotFoundException("Queued investigation order was not found.");
        if (!context.ServicePoint.IsActive)
            throw new DomainRuleException("An inactive imaging service point cannot register a study.");
        if (context.QueueTicket.Status is QueueTicketStatus.Completed or QueueTicketStatus.Cancelled)
            throw new DomainRuleException("A completed or cancelled Queue ticket cannot register a study.");
        await EnsureEligibleAsync(branch, context.Order.RequestedServiceId, cancellationToken);
        var now = clock.UtcNow;
        var study = RadiologyStudy.Register(context.Order, request.RequestId, actor.ActorId, now);
        var studyEvent = RadiologyStudyEvent.Record(study, request.RequestId, fingerprint, RegisteredAction,
            actor.ActorId, now);
        var result = await repository.RegisterAsync(study, studyEvent,
            Audit(branch, study.Id, "RadiologyStudy.Registered", new
            {
                study.OrderId,
                study.ServiceId,
                study.Modality,
                study.Version
            }, now), cancellationToken);
        return Map(result.Aggregate, result.IsReplay);
    }

    public async Task<RadiologyStudyResponse> StartAsync(long branchId, long studyId,
        StartRadiologyStudyRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.RadiologyStudiesStart, branchId,
            cancellationToken);
        var fingerprint = Fingerprint(new { Action = StartedAction, StudyId = studyId, request.ExpectedVersion });
        var replay = await FindReplayAsync(branchId, null, studyId, request.RequestId, StartedAction,
            fingerprint, cancellationToken);
        if (replay is not null) return replay;
        var aggregate = await RequireStudyAsync(branchId, studyId, true, cancellationToken);
        await EnsureEligibleAsync(branch, aggregate.Study.ServiceId, cancellationToken);
        var now = clock.UtcNow;
        aggregate.Study.Start(request.ExpectedVersion, actor.ActorId, now);
        var studyEvent = RadiologyStudyEvent.Record(aggregate.Study, request.RequestId, fingerprint, StartedAction,
            actor.ActorId, now);
        var result = await repository.StartAsync(aggregate.Study, studyEvent,
            Audit(branch, studyId, "RadiologyStudy.Started", new
            {
                aggregate.Study.Status,
                aggregate.Study.Version,
                AttemptSequence = aggregate.Study.AcquisitionAttemptCount + 1
            }, now), cancellationToken);
        return Map(result.Aggregate, result.IsReplay);
    }

    public async Task<RadiologyStudyResponse> RecordAcquisitionAsync(long branchId, long studyId,
        RecordRadiologyAcquisitionRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.RadiologyAcquisitionsRecord, branchId,
            cancellationToken);
        if (!Enum.TryParse<RadiologyAcquisitionOutcome>(request.Outcome, true, out var outcome))
            throw new DomainRuleException("Radiology acquisition outcome must be Acquired or Aborted.");
        var equipmentId = publicIds.Decode(PublicIdKind.BookableResource, request.EquipmentResourceId,
            branch.TenantId);
        var normalized = new
        {
            Action = AcquisitionRecordedAction,
            StudyId = studyId,
            request.ExpectedVersion,
            EquipmentId = equipmentId,
            ProtocolCode = (request.ProtocolCode ?? string.Empty).Trim().ToUpperInvariant(),
            ProtocolVersion = (request.ProtocolVersion ?? string.Empty).Trim().ToUpperInvariant(),
            request.HasProtocolDeviation,
            DeviationCode = request.DeviationCode?.Trim().ToUpperInvariant(),
            DeviationNote = request.DeviationNote?.Trim(),
            Outcome = outcome.ToString(),
            OutcomeReasonCode = request.OutcomeReasonCode?.Trim().ToUpperInvariant(),
            OutcomeNote = request.OutcomeNote?.Trim(),
            ExternalStudyReference = request.ExternalStudyReference?.Trim(),
            request.StartedUtc,
            request.CompletedUtc
        };
        var fingerprint = Fingerprint(normalized);
        var replay = await FindReplayAsync(branchId, null, studyId, request.RequestId,
            AcquisitionRecordedAction, fingerprint, cancellationToken);
        if (replay is not null) return replay;

        var aggregate = await RequireStudyAsync(branchId, studyId, true, cancellationToken);
        await EnsureEligibleAsync(branch, aggregate.Study.ServiceId, cancellationToken);
        var orderContext = await repository.GetOrderContextAsync(branchId, aggregate.Study.OrderId,
            cancellationToken) ?? throw new NotFoundException("Queued investigation order was not found.");
        if (orderContext.ServicePoint.ResourceId.HasValue
            && orderContext.ServicePoint.ResourceId.Value != equipmentId)
            throw new DomainRuleException("The equipment does not match the resource assigned to the imaging service point.");
        _ = await repository.GetEligibleEquipmentAsync(branchId, equipmentId, aggregate.Study.ServiceId,
                aggregate.Study.Modality, cancellationToken)
            ?? throw new DomainRuleException(
                "Equipment must be an active, available matching-modality resource with the ordered service capability.");
        var now = clock.UtcNow;
        var attempt = aggregate.Study.RecordAcquisition(request.ExpectedVersion, request.RequestId, equipmentId,
            request.ProtocolCode ?? string.Empty, request.ProtocolVersion ?? string.Empty,
            request.HasProtocolDeviation, request.DeviationCode,
            request.DeviationNote, outcome, request.OutcomeReasonCode, request.OutcomeNote,
            request.ExternalStudyReference, actor.ActorId, request.StartedUtc, request.CompletedUtc, now);
        var studyEvent = RadiologyStudyEvent.Record(aggregate.Study, request.RequestId, fingerprint,
            AcquisitionRecordedAction, actor.ActorId, now);
        var result = await repository.RecordAcquisitionAsync(aggregate.Study, attempt, studyEvent,
            Audit(branch, studyId, "RadiologyStudy.AcquisitionRecorded", new
            {
                attempt.Id,
                attempt.Sequence,
                attempt.EquipmentResourceId,
                attempt.ProtocolCode,
                attempt.ProtocolVersion,
                attempt.HasProtocolDeviation,
                attempt.Outcome,
                aggregate.Study.Status,
                aggregate.Study.Version
            }, now), cancellationToken);
        return Map(result.Aggregate, result.IsReplay);
    }

    public async Task<RadiologyStudyResponse> ReviewQualityAsync(long branchId, long studyId,
        ReviewRadiologyQualityRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.RadiologyStudiesQualityReview, branchId,
            cancellationToken);
        if (!Enum.TryParse<RadiologyQualityDecision>(request.Decision, true, out var decision))
            throw new DomainRuleException("Radiology quality decision must be Accepted or RepeatRequired.");
        var attemptId = publicIds.Decode(PublicIdKind.RadiologyAcquisitionAttempt,
            request.AcquisitionAttemptId, branch.TenantId);
        var action = decision == RadiologyQualityDecision.Accepted
            ? QualityAcceptedAction
            : RepeatRequiredAction;
        var fingerprint = Fingerprint(new
        {
            Action = action,
            StudyId = studyId,
            request.ExpectedVersion,
            AttemptId = attemptId,
            Decision = decision.ToString(),
            ReasonCode = (request.ReasonCode ?? string.Empty).Trim().ToUpperInvariant(),
            Note = request.Note?.Trim()
        });
        var replay = await FindReplayAsync(branchId, null, studyId, request.RequestId, action,
            fingerprint, cancellationToken);
        if (replay is not null) return replay;

        var aggregate = await RequireStudyAsync(branchId, studyId, true, cancellationToken);
        await EnsureEligibleAsync(branch, aggregate.Study.ServiceId, cancellationToken);
        var attempt = aggregate.AcquisitionAttempts.SingleOrDefault(item => item.Id == attemptId)
            ?? throw new NotFoundException("Radiology acquisition attempt was not found.");
        var now = clock.UtcNow;
        var review = aggregate.Study.ReviewQuality(request.ExpectedVersion, attempt, request.RequestId,
            decision, request.ReasonCode ?? string.Empty, request.Note, actor.ActorId, now);
        var studyEvent = RadiologyStudyEvent.Record(aggregate.Study, request.RequestId, fingerprint, action,
            actor.ActorId, now);
        var result = await repository.ReviewQualityAsync(aggregate.Study, review, studyEvent,
            Audit(branch, studyId, decision == RadiologyQualityDecision.Accepted
                ? "RadiologyStudy.QualityAccepted"
                : "RadiologyStudy.RepeatRequired", new
                {
                    review.Id,
                    review.AcquisitionAttemptId,
                    review.Decision,
                    review.ReasonCode,
                    aggregate.Study.Status,
                    aggregate.Study.Version
                }, now), cancellationToken);
        return Map(result.Aggregate, result.IsReplay);
    }

    private async Task<RadiologyStudyResponse?> FindReplayAsync(long branchId, long? expectedOrderId,
        long? expectedStudyId, Guid requestId, string action, string fingerprint,
        CancellationToken cancellationToken)
    {
        if (requestId == Guid.Empty) throw new DomainRuleException("A non-empty request identity is required.");
        var prior = await repository.GetEventByRequestIdAsync(requestId, cancellationToken);
        if (prior is null) return null;
        if (!string.Equals(prior.Action, action, StringComparison.Ordinal)
            || !string.Equals(prior.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            throw new DomainRuleException("The radiology request identifier was reused with different content.");
        if (expectedStudyId.HasValue && prior.StudyId != expectedStudyId.Value)
            throw new DomainRuleException("The radiology request identifier belongs to another study.");
        var aggregate = await repository.GetByIdAsync(branchId, prior.StudyId, false, cancellationToken)
            ?? throw new NotFoundException("Radiology study was not found.");
        if (expectedOrderId.HasValue && aggregate.Study.OrderId != expectedOrderId.Value)
            throw new DomainRuleException("The radiology request identifier belongs to another order.");
        return Map(aggregate, true);
    }

    private async Task<RadiologyStudyAggregate> RequireStudyAsync(long branchId, long studyId, bool tracked,
        CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(branchId, studyId, tracked, cancellationToken)
        ?? throw new NotFoundException("Radiology study was not found.");

    private async Task<Branch> RequireBranchAsync(string permission, long branchId,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this radiology operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private Task EnsureEligibleAsync(Branch branch, long serviceId, CancellationToken cancellationToken)
    {
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId)).DateTime);
        return practitionerEligibility.EnsureEligiblePerformerAsync(actor.ActorId, branch.Id, serviceId,
            localDate, cancellationToken);
    }

    private AuditEvent Audit(Branch branch, long entityId, string action, object metadata,
        DateTimeOffset now) => AuditEvent.Record(branch.TenantId, branch.Id, actor.ActorId, action,
        nameof(RadiologyStudy), entityId, JsonSerializer.Serialize(metadata), correlation.CorrelationId, now);

    private static string Fingerprint(object value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));

    private RadiologyStudyResponse Map(RadiologyStudyAggregate aggregate, bool isReplay)
    {
        var study = aggregate.Study;
        var acquisitionAttemptIds = aggregate.AcquisitionAttempts.ToDictionary(
            item => item.Id,
            item => publicIds.Encode(PublicIdKind.RadiologyAcquisitionAttempt, item.Id, item.TenantId));
        var acquisitionAttempts = aggregate.AcquisitionAttempts.OrderBy(item => item.Sequence).Select(item =>
            new RadiologyAcquisitionAttemptResponse(
                acquisitionAttemptIds[item.Id],
                item.Sequence,
                publicIds.Encode(PublicIdKind.BookableResource, item.EquipmentResourceId, item.TenantId),
                item.ProtocolCode,
                item.ProtocolVersion,
                item.HasProtocolDeviation,
                item.DeviationCode,
                item.DeviationNote,
                item.Outcome.ToString(),
                item.OutcomeReasonCode,
                item.OutcomeNote,
                item.ExternalStudyReference is not null,
                publicIds.Encode(PublicIdKind.IdentitySubject, item.PerformedByActorId),
                item.StartedUtc,
                item.CompletedUtc)).ToArray();
        var qualityReviews = aggregate.QualityReviews.OrderBy(item => item.CreatedUtc).Select(item =>
            new RadiologyQualityReviewResponse(
                publicIds.Encode(PublicIdKind.RadiologyQualityReview, item.Id, item.TenantId),
                acquisitionAttemptIds[item.AcquisitionAttemptId],
                item.Decision.ToString(),
                item.ReasonCode,
                item.Note,
                publicIds.Encode(PublicIdKind.IdentitySubject, item.ReviewedByActorId),
                item.CreatedUtc)).ToArray();
        return new(
            publicIds.Encode(PublicIdKind.RadiologyStudy, study.Id, study.TenantId),
            publicIds.Encode(PublicIdKind.InvestigationOrder, study.OrderId, study.TenantId),
            publicIds.Encode(PublicIdKind.Patient, study.PatientId, study.TenantId),
            publicIds.Encode(PublicIdKind.ClinicalService, study.ServiceId, study.TenantId),
            study.Modality.ToString(),
            study.Status.ToString(),
            publicIds.Encode(PublicIdKind.IdentitySubject, study.RegisteredByActorId),
            study.RegisteredUtc,
            study.LastStartedByActorId.HasValue
                ? publicIds.Encode(PublicIdKind.IdentitySubject, study.LastStartedByActorId.Value)
                : null,
            study.LastStartedUtc,
            study.AcquisitionAttemptCount,
            study.Version,
            acquisitionAttempts,
            qualityReviews,
            aggregate.History.OrderBy(item => item.StudyVersion).Select(item =>
                new RadiologyStudyEventResponse(
                    publicIds.Encode(PublicIdKind.RadiologyStudyEvent, item.Id, item.TenantId),
                    item.Action,
                    publicIds.Encode(PublicIdKind.IdentitySubject, item.ActorId),
                    item.StudyVersion,
                    item.CreatedUtc)).ToArray(),
            isReplay);
    }
}
