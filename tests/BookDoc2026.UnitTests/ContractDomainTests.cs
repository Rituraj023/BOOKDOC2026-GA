using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Contracts;

namespace BookDoc2026.UnitTests;

public sealed class ContractDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reservation_ConsumesUnitsAndMaintainsLedgerInvariant()
    {
        var (agreement, entitlement) = CreateEntitlement(10);
        var reservation = EntitlementReservation.Reserve(
            agreement, entitlement, NumericId.Next(), Guid.NewGuid(), "HASH", 3, 1, Now);

        Assert.Equal(3, entitlement.ReservedUnits);
        Assert.Equal(7, entitlement.AvailableUnits);

        reservation.Consume(entitlement, 1, 2, Now.AddMinutes(1));

        Assert.Equal(EntitlementReservationStatus.Consumed, reservation.Status);
        Assert.Equal(0, entitlement.ReservedUnits);
        Assert.Equal(3, entitlement.ConsumedUnits);
        Assert.Equal(7, entitlement.AvailableUnits);
    }

    [Fact]
    public void Reservation_ReleaseRestoresAvailabilityAndRequiresReason()
    {
        var (agreement, entitlement) = CreateEntitlement(5);
        var reservation = EntitlementReservation.Reserve(
            agreement, entitlement, NumericId.Next(), Guid.NewGuid(), "HASH", 2, 1, Now);

        Assert.Throws<DomainRuleException>(() =>
            reservation.Release(entitlement, 1, 2, " ", Now.AddMinutes(1)));
        Assert.Equal(2, entitlement.ReservedUnits);

        reservation.Release(entitlement, 1, 2, "Booking cancelled", Now.AddMinutes(2));

        Assert.Equal(EntitlementReservationStatus.Released, reservation.Status);
        Assert.Equal(5, entitlement.AvailableUnits);
        Assert.Equal("Booking cancelled", reservation.ReleaseReason);
    }

    [Fact]
    public void Reservation_RejectsInsufficientUnitsAndStaleVersion()
    {
        var (agreement, entitlement) = CreateEntitlement(2);

        Assert.Throws<DomainRuleException>(() => EntitlementReservation.Reserve(
            agreement, entitlement, NumericId.Next(), Guid.NewGuid(), "HASH", 3, 1, Now));
        Assert.Throws<ConcurrencyConflictException>(() => EntitlementReservation.Reserve(
            agreement, entitlement, NumericId.Next(), Guid.NewGuid(), "HASH", 1, 2, Now));
        Assert.Equal(2, entitlement.AvailableUnits);
    }

    [Fact]
    public void Contract_EffectivenessIsInclusiveAndStatusAware()
    {
        var (agreement, _) = CreateEntitlement(1);

        Assert.True(agreement.IsEffective(new DateOnly(2026, 8, 1)));
        Assert.True(agreement.IsEffective(new DateOnly(2026, 8, 31)));
        Assert.False(agreement.IsEffective(new DateOnly(2026, 9, 1)));
    }

    private static (ContractAgreement Agreement, ContractEntitlement Entitlement) CreateEntitlement(int units)
    {
        var agreement = ContractAgreement.Create(
            NumericId.Next(), NumericId.Next(), NumericId.Next(), "CNT-0001", "PHYSIO",
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), 5000m, "INR", "v1", null, Now);
        var entitlement = ContractEntitlement.Create(
            agreement, NumericId.Next(), null, units, 500m, "INR", "v1", Now);
        return (agreement, entitlement);
    }
}
