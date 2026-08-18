using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Scheduling;

public sealed class SchedulingService(
    ISchedulingRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlationContext)
{
    public async Task<AvailabilityRuleResponse> CreateRuleAsync(long branchId, CreateAvailabilityRuleRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingAvailabilityManage, branchId, cancellationToken);
        var resourceId = publicIds.Decode(PublicIdKind.BookableResource, request.ResourceId, branch.TenantId);
        var serviceId = publicIds.DecodeOptional(PublicIdKind.ClinicalService, request.ServiceId, branch.TenantId);
        var resource = await repository.GetResourceAsync(branchId, resourceId, cancellationToken)
            ?? throw new NotFoundException("Resource was not found in this branch.");
        if (serviceId.HasValue && !await repository.ResourceSupportsServiceAsync(resource.Id, serviceId.Value, cancellationToken))
            throw new DomainRuleException("The resource does not support the selected service.");
        if (!Enum.TryParse<DayOfWeek>(request.DayOfWeek, true, out var day))
            throw new DomainRuleException("Day of week is invalid.");
        var rule = AvailabilityRule.Create(branch.TenantId, branchId, resource.Id, serviceId, day,
            request.LocalStart, request.LocalEnd, request.EffectiveFrom, request.EffectiveTo,
            request.SlotIntervalMinutes, request.Capacity, clock.UtcNow);
        await repository.AddRuleAsync(rule, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task<AvailabilityExceptionResponse> CreateExceptionAsync(long branchId, CreateAvailabilityExceptionRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingAvailabilityManage, branchId, cancellationToken);
        var resourceId = publicIds.Decode(PublicIdKind.BookableResource, request.ResourceId, branch.TenantId);
        _ = await repository.GetResourceAsync(branchId, resourceId, cancellationToken)
            ?? throw new NotFoundException("Resource was not found in this branch.");
        if (!Enum.TryParse<AvailabilityExceptionKind>(request.Kind, true, out var kind))
            throw new DomainRuleException("Availability exception kind is invalid.");
        var item = AvailabilityException.Create(branch.TenantId, branchId, resourceId,
            request.StartUtc, request.EndUtc, kind, request.CapacityOverride, request.Reason, clock.UtcNow);
        await repository.AddExceptionAsync(item, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<IReadOnlyCollection<AvailabilityResourceResponse>> SearchAvailabilityAsync(
        long branchId, long serviceId, DateTimeOffset startUtc, DateTimeOffset endUtc, int quantity,
        CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingAvailabilityView, branchId, cancellationToken);
        if (startUtc >= endUtc || startUtc <= clock.UtcNow || quantity is < 1 or > 1000)
            throw new DomainRuleException("Availability search interval or quantity is invalid.");
        if (!await repository.ServiceExistsAsync(serviceId, cancellationToken))
            throw new NotFoundException("Service was not found.");

        var resources = await repository.ListServiceResourcesAsync(branchId, serviceId, cancellationToken);
        var ids = resources.Select(resource => resource.Id).ToArray();
        var rules = await repository.ListRulesAsync(branchId, ids, cancellationToken);
        var exceptions = await repository.ListExceptionsAsync(branchId, ids, startUtc, endUtc, cancellationToken);
        var reserved = await repository.GetReservedQuantitiesAsync(ids, startUtc, endUtc, clock.UtcNow, cancellationToken);
        return resources.Select(resource => Evaluate(resource, serviceId, startUtc, endUtc, quantity, rules, exceptions,
            reserved.GetValueOrDefault(resource.Id))).ToArray();
    }

    public async Task<SchedulingHoldResponse> CreateHoldAsync(long branchId, CreateSchedulingHoldRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.SchedulingHoldsCreate, branchId, cancellationToken);
        var patientId = publicIds.Decode(PublicIdKind.Patient, request.PatientId, branch.TenantId);
        var serviceId = publicIds.Decode(PublicIdKind.ClinicalService, request.ServiceId, branch.TenantId);
        var resources = request.Resources.Select(item => new
        {
            ResourceId = publicIds.Decode(PublicIdKind.BookableResource, item.ResourceId, branch.TenantId),
            item.Quantity
        }).ToArray();
        if (!await repository.PatientExistsAsync(patientId, cancellationToken)) throw new NotFoundException("Patient was not found.");
        if (!await repository.ServiceExistsAsync(serviceId, cancellationToken)) throw new NotFoundException("Service was not found.");
        if (resources.Length == 0 || resources.Select(item => item.ResourceId).Distinct().Count() != resources.Length)
            throw new DomainRuleException("At least one unique resource is required for a hold.");

        var now = clock.UtcNow;
        var canonical = JsonSerializer.Serialize(new
        {
            PatientId = patientId,
            ServiceId = serviceId,
            request.StartUtc,
            request.EndUtc,
            request.HoldMinutes,
            Resources = resources.OrderBy(item => item.ResourceId).Select(item => new { item.ResourceId, item.Quantity })
        });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var hold = SchedulingHold.Create(branch.TenantId, branchId, patientId, serviceId,
            request.RequestId, hash, request.StartUtc, request.EndUtc, now.AddMinutes(request.HoldMinutes), now);
        var reservations = resources.Select(item => ResourceReservation.Create(branch.TenantId, branchId,
            hold.Id, item.ResourceId, request.StartUtc, request.EndUtc, item.Quantity, now)).ToArray();
        var audit = AuditEvent.Record(branch.TenantId, branchId, actor.ActorId, "SchedulingHold.Created",
            nameof(SchedulingHold), hold.Id, JsonSerializer.Serialize(new { PatientId = patientId, ServiceId = serviceId, ResourceCount = reservations.Length }),
            correlationContext.CorrelationId, now);
        var result = await repository.CreateHoldAtomicallyAsync(hold, reservations, audit, now, cancellationToken);
        return Map(result.Aggregate);
    }

    public async Task<SchedulingHoldResponse> GetHoldAsync(long branchId, long holdId, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingAvailabilityView, branchId, cancellationToken);
        var aggregate = await repository.GetHoldAsync(branchId, holdId, cancellationToken)
            ?? throw new NotFoundException("Scheduling hold was not found.");
        aggregate.Hold.MarkExpired(clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    public async Task<SchedulingHoldResponse> ReleaseHoldAsync(long branchId, long holdId, ReleaseSchedulingHoldRequest request, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.SchedulingHoldsRelease, branchId, cancellationToken);
        var aggregate = await repository.GetHoldAsync(branchId, holdId, cancellationToken)
            ?? throw new NotFoundException("Scheduling hold was not found.");
        aggregate.Hold.MarkExpired(clock.UtcNow);
        aggregate.Hold.Release(request.ExpectedVersion, clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    private async Task<Branch> RequireBranchAsync(string permission, long branchId, CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this scheduling operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private AvailabilityResourceResponse Evaluate(BookDoc2026.Domain.Catalog.BookableResource resource, long serviceId,
        DateTimeOffset startUtc, DateTimeOffset endUtc, int requested, IReadOnlyCollection<AvailabilityRule> rules,
        IReadOnlyCollection<AvailabilityException> exceptions, int reserved)
    {
        var reason = SchedulingAvailabilityEvaluator.GetUnavailableReason(resource, serviceId, startUtc, endUtc, requested, reserved, rules, exceptions, out var capacity);
        return new(publicIds.Encode(PublicIdKind.BookableResource, resource.Id, resource.TenantId), resource.Code, resource.Name, capacity, reserved, Math.Max(0, capacity - reserved), reason is null, reason);
    }

    private AvailabilityRuleResponse Map(AvailabilityRule rule) => new(
        publicIds.Encode(PublicIdKind.AvailabilityRule, rule.Id, rule.TenantId),
        publicIds.Encode(PublicIdKind.BookableResource, rule.ResourceId, rule.TenantId),
        publicIds.EncodeOptional(PublicIdKind.ClinicalService, rule.ServiceId, rule.TenantId),
        rule.DayOfWeek.ToString(), rule.LocalStart, rule.LocalEnd, rule.EffectiveFrom, rule.EffectiveTo,
        rule.SlotIntervalMinutes, rule.Capacity, rule.IsActive, rule.Version);
    private AvailabilityExceptionResponse Map(AvailabilityException item) => new(
        publicIds.Encode(PublicIdKind.AvailabilityException, item.Id, item.TenantId),
        publicIds.Encode(PublicIdKind.BookableResource, item.ResourceId, item.TenantId),
        item.StartUtc, item.EndUtc, item.Kind.ToString(), item.CapacityOverride, item.Reason);
    private SchedulingHoldResponse Map(SchedulingHoldAggregate aggregate) => new(
        publicIds.Encode(PublicIdKind.SchedulingHold, aggregate.Hold.Id, aggregate.Hold.TenantId),
        aggregate.Hold.RequestId,
        publicIds.Encode(PublicIdKind.Patient, aggregate.Hold.PatientId, aggregate.Hold.TenantId),
        publicIds.Encode(PublicIdKind.ClinicalService, aggregate.Hold.ServiceId, aggregate.Hold.TenantId), aggregate.Hold.StartUtc,
        aggregate.Hold.EndUtc, aggregate.Hold.ExpiresUtc, aggregate.Hold.Status.ToString(), aggregate.Hold.Version,
        aggregate.Reservations.Select(item => new HoldResourceResponse(
            publicIds.Encode(PublicIdKind.BookableResource, item.ResourceId, item.TenantId), item.Quantity)).ToArray());
}
