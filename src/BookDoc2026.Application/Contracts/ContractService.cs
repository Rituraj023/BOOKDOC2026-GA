using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Contracts;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Contracts;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Contracts;

public sealed class ContractService(
    IContractRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlation)
{
    public async Task<ContractResponse> CreateAsync(long branchId, CreateContractRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.ContractsManage, branchId, cancellationToken);
        var patientId = publicIds.Decode(PublicIdKind.Patient, request.PatientId, branch.TenantId);
        if (!await repository.PatientExistsAsync(patientId, cancellationToken))
            throw new NotFoundException("Patient was not found in the current tenant.");
        if (request.Entitlements.Count == 0)
            throw new DomainRuleException("At least one contract entitlement is required.");
        if (await repository.ContractNumberExistsAsync(branchId, request.ContractNumber.Trim(), cancellationToken))
            throw new DomainRuleException("Contract number already exists in this branch.");

        var decoded = request.Entitlements.Select(item => new
        {
            Request = item,
            ServiceId = publicIds.Decode(PublicIdKind.ClinicalService, item.ServiceId, branch.TenantId),
            ResourceCategoryId = publicIds.DecodeOptional(PublicIdKind.ResourceCategory, item.ResourceCategoryId, branch.TenantId)
        }).ToArray();
        if (decoded.Select(item => (item.ServiceId, item.ResourceCategoryId)).Distinct().Count() != decoded.Length)
            throw new DomainRuleException("Contract entitlement service and resource-category combinations must be unique.");
        foreach (var item in decoded)
        {
            if (!await repository.ServiceExistsAsync(item.ServiceId, cancellationToken))
                throw new NotFoundException("An entitlement service was not found.");
            if (item.ResourceCategoryId.HasValue
                && !await repository.ResourceCategoryExistsAsync(item.ResourceCategoryId.Value, cancellationToken))
                throw new NotFoundException("An entitlement resource category was not found.");
        }

        var now = clock.UtcNow;
        var agreement = ContractAgreement.Create(branch.TenantId, branchId, patientId, request.ContractNumber,
            request.ContractTypeCode, request.ValidFrom, request.ValidTo, request.PackagePrice, request.Currency,
            request.RuleVersion, request.Notes, now);
        var entitlements = decoded.Select(item => ContractEntitlement.Create(agreement, item.ServiceId,
            item.ResourceCategoryId, item.Request.TotalUnits, item.Request.UnitPrice, request.Currency,
            item.Request.RuleVersion, now)).ToArray();
        var audit = AuditEvent.Record(branch.TenantId, branchId, actor.ActorId, "Contract.Created",
            nameof(ContractAgreement), agreement.Id,
            JsonSerializer.Serialize(new { agreement.PatientId, agreement.ContractNumber, EntitlementCount = entitlements.Length }),
            correlation.CorrelationId, now);
        await repository.AddContractAsync(agreement, entitlements, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new ContractAggregate(agreement, entitlements));
    }

    public async Task<ContractResponse> GetAsync(long branchId, long contractId, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(FoundationPermissions.ContractsView, branchId, cancellationToken);
        var aggregate = await repository.GetContractAsync(branchId, contractId, false, cancellationToken)
            ?? throw new NotFoundException("Contract was not found.");
        return Map(aggregate);
    }

    public async Task<EntitlementReservationResponse> ReserveAsync(long branchId, long contractId, long entitlementId,
        ReserveEntitlementRequest request, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(FoundationPermissions.ContractEntitlementsReserve, branchId, cancellationToken);
        var bookingId = publicIds.Decode(PublicIdKind.Booking, request.BookingId, branch.TenantId);
        var hash = Hash(new { contractId, entitlementId, bookingId, request.Units });
        var existing = await repository.GetReservationByRequestAsync(branchId, request.RequestId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.Reservation.RequestHash, hash, StringComparison.Ordinal))
                throw new DomainRuleException("The entitlement request identifier was reused with different content.");
            return Map(existing, true);
        }

        var contract = await repository.GetContractAsync(branchId, contractId, true, cancellationToken)
            ?? throw new NotFoundException("Contract was not found.");
        var entitlement = contract.Entitlements.SingleOrDefault(item => item.Id == entitlementId)
            ?? throw new NotFoundException("Entitlement was not found on this contract.");
        var booking = await repository.GetBookingAsync(branchId, bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking was not found.");
        if (booking.Status != BookingStatus.Confirmed)
            throw new DomainRuleException("Only a confirmed booking can reserve contract entitlement units.");
        if (booking.PatientId != contract.Agreement.PatientId || booking.ServiceId != entitlement.ServiceId)
            throw new DomainRuleException("Booking patient or service does not match the selected entitlement.");
        if (entitlement.ResourceCategoryId.HasValue
            && !await repository.BookingUsesResourceCategoryAsync(bookingId, entitlement.ResourceCategoryId.Value, cancellationToken))
            throw new DomainRuleException("Booking resources do not satisfy the entitlement resource category.");
        var serviceDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(booking.StartUtc,
            TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId)).DateTime);
        if (!contract.Agreement.IsEffective(serviceDate))
            throw new DomainRuleException("Contract is not active for the booking service date.");

        var now = clock.UtcNow;
        var reservation = EntitlementReservation.Reserve(contract.Agreement, entitlement, bookingId,
            request.RequestId, hash, request.Units, request.ExpectedEntitlementVersion, now);
        var audit = AuditEvent.Record(branch.TenantId, branchId, actor.ActorId, "ContractEntitlement.Reserved",
            nameof(EntitlementReservation), reservation.Id,
            JsonSerializer.Serialize(new { reservation.ContractId, reservation.EntitlementId, reservation.BookingId, reservation.Units }),
            correlation.CorrelationId, now);
        await repository.AddReservationAsync(reservation, audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(new(reservation, entitlement), false);
    }

    public Task<EntitlementReservationResponse> ConsumeAsync(long branchId, long reservationId,
        ConsumeEntitlementReservationRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, reservationId, FoundationPermissions.ContractEntitlementsConsume,
            "ContractEntitlement.Consumed", (aggregate, now) => aggregate.Reservation.Consume(
                aggregate.Entitlement, request.ExpectedReservationVersion, request.ExpectedEntitlementVersion, now),
            cancellationToken);

    public Task<EntitlementReservationResponse> ReleaseAsync(long branchId, long reservationId,
        ReleaseEntitlementReservationRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, reservationId, FoundationPermissions.ContractEntitlementsRelease,
            "ContractEntitlement.Released", (aggregate, now) => aggregate.Reservation.Release(
                aggregate.Entitlement, request.ExpectedReservationVersion, request.ExpectedEntitlementVersion,
                request.Reason, now), cancellationToken);

    private async Task<EntitlementReservationResponse> TransitionAsync(long branchId, long reservationId,
        string permission, string action, Action<EntitlementReservationAggregate, DateTimeOffset> transition,
        CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(permission, branchId, cancellationToken);
        var aggregate = await repository.GetReservationAsync(branchId, reservationId, true, cancellationToken)
            ?? throw new NotFoundException("Entitlement reservation was not found.");
        var now = clock.UtcNow;
        transition(aggregate, now);
        var audit = AuditEvent.Record(branch.TenantId, branchId, actor.ActorId, action,
            nameof(EntitlementReservation), aggregate.Reservation.Id,
            JsonSerializer.Serialize(new { aggregate.Reservation.ContractId, aggregate.Reservation.EntitlementId,
                aggregate.Reservation.BookingId, aggregate.Reservation.Units }), correlation.CorrelationId, now);
        await repository.AddAuditAsync(audit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate, false);
    }

    private async Task<Branch> RequireBranchAsync(string permission, long branchId, CancellationToken cancellationToken)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
            throw new ForbiddenException("The actor is not authorized for this contract operation.");
        return await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
    }

    private ContractResponse Map(ContractAggregate aggregate) => new(
        publicIds.Encode(PublicIdKind.Contract, aggregate.Agreement.Id, aggregate.Agreement.TenantId),
        publicIds.Encode(PublicIdKind.Patient, aggregate.Agreement.PatientId, aggregate.Agreement.TenantId),
        aggregate.Agreement.ContractNumber, aggregate.Agreement.ContractTypeCode, aggregate.Agreement.ValidFrom,
        aggregate.Agreement.ValidTo, aggregate.Agreement.Status.ToString(), aggregate.Agreement.PackagePrice,
        aggregate.Agreement.Currency, aggregate.Agreement.RuleVersion, aggregate.Agreement.Notes,
        aggregate.Agreement.Version, aggregate.Entitlements.Select(Map).ToArray());

    private ContractEntitlementResponse Map(ContractEntitlement item) => new(
        publicIds.Encode(PublicIdKind.ContractEntitlement, item.Id, item.TenantId),
        publicIds.Encode(PublicIdKind.ClinicalService, item.ServiceId, item.TenantId),
        publicIds.EncodeOptional(PublicIdKind.ResourceCategory, item.ResourceCategoryId, item.TenantId),
        item.TotalUnits, item.ReservedUnits, item.ConsumedUnits, item.AvailableUnits, item.UnitPrice,
        item.Currency, item.RuleVersion, item.Version);

    private EntitlementReservationResponse Map(EntitlementReservationAggregate aggregate, bool isReplay) => new(
        publicIds.Encode(PublicIdKind.EntitlementReservation, aggregate.Reservation.Id, aggregate.Reservation.TenantId),
        publicIds.Encode(PublicIdKind.Contract, aggregate.Reservation.ContractId, aggregate.Reservation.TenantId),
        publicIds.Encode(PublicIdKind.ContractEntitlement, aggregate.Reservation.EntitlementId, aggregate.Reservation.TenantId),
        publicIds.Encode(PublicIdKind.Booking, aggregate.Reservation.BookingId, aggregate.Reservation.TenantId),
        aggregate.Reservation.RequestId, aggregate.Reservation.Units, aggregate.Reservation.Status.ToString(),
        aggregate.Reservation.ReservedUtc, aggregate.Reservation.ConsumedUtc, aggregate.Reservation.ReleasedUtc,
        aggregate.Reservation.ReleaseReason, aggregate.Reservation.Version, Map(aggregate.Entitlement), isReplay);

    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
}
