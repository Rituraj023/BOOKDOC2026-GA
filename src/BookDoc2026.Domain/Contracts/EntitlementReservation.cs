using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Contracts;

public sealed class EntitlementReservation : TenantScopedEntity
{
    private EntitlementReservation() { }

    public long BranchId { get; private set; }
    public long ContractId { get; private set; }
    public long EntitlementId { get; private set; }
    public long BookingId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public int Units { get; private set; }
    public EntitlementReservationStatus Status { get; private set; }
    public DateTimeOffset ReservedUtc { get; private set; }
    public DateTimeOffset? ConsumedUtc { get; private set; }
    public DateTimeOffset? ReleasedUtc { get; private set; }
    public string? ReleaseReason { get; private set; }
    public long Version { get; private set; } = 1;

    public static EntitlementReservation Reserve(
        ContractAgreement agreement,
        ContractEntitlement entitlement,
        long bookingId,
        Guid requestId,
        string requestHash,
        int units,
        long expectedEntitlementVersion,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(agreement);
        ArgumentNullException.ThrowIfNull(entitlement);
        if (bookingId <= 0 || requestId == Guid.Empty || string.IsNullOrWhiteSpace(requestHash))
            throw new DomainRuleException("Booking, request identity and request hash are required.");
        if (agreement.Id != entitlement.ContractId || agreement.TenantId != entitlement.TenantId
            || agreement.BranchId != entitlement.BranchId)
            throw new DomainRuleException("Entitlement does not belong to the selected contract.");
        entitlement.Reserve(units, expectedEntitlementVersion, now);
        var reservation = new EntitlementReservation
        {
            TenantId = agreement.TenantId,
            BranchId = agreement.BranchId,
            ContractId = agreement.Id,
            EntitlementId = entitlement.Id,
            BookingId = bookingId,
            RequestId = requestId,
            RequestHash = requestHash,
            Units = units,
            Status = EntitlementReservationStatus.Reserved,
            ReservedUtc = now
        };
        reservation.StampCreated(now);
        return reservation;
    }

    public void Consume(ContractEntitlement entitlement, long expectedReservationVersion, long expectedEntitlementVersion, DateTimeOffset now)
    {
        EnsureEntitlement(entitlement);
        EnsureReserved(expectedReservationVersion);
        entitlement.ConsumeReserved(Units, expectedEntitlementVersion, now);
        Status = EntitlementReservationStatus.Consumed;
        ConsumedUtc = now;
        Version++;
        StampModified(now);
    }

    public void Release(ContractEntitlement entitlement, long expectedReservationVersion, long expectedEntitlementVersion, string reason, DateTimeOffset now)
    {
        EnsureEntitlement(entitlement);
        EnsureReserved(expectedReservationVersion);
        var normalizedReason = ContractAgreement.Normalize(reason, 3, 250, "Release reason");
        entitlement.ReleaseReserved(Units, expectedEntitlementVersion, now);
        Status = EntitlementReservationStatus.Released;
        ReleasedUtc = now;
        ReleaseReason = normalizedReason;
        Version++;
        StampModified(now);
    }

    private void EnsureEntitlement(ContractEntitlement entitlement)
    {
        ArgumentNullException.ThrowIfNull(entitlement);
        if (EntitlementId != entitlement.Id || ContractId != entitlement.ContractId || TenantId != entitlement.TenantId)
            throw new DomainRuleException("Entitlement does not belong to this reservation.");
    }

    private void EnsureReserved(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The entitlement reservation changed after it was loaded.");
        if (Status != EntitlementReservationStatus.Reserved)
            throw new DomainRuleException("Only a reserved entitlement can be consumed or released.");
    }
}
