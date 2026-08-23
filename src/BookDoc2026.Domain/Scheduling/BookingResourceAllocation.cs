using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class BookingResourceAllocation : TenantScopedEntity
{
    private BookingResourceAllocation() { }

    public long BranchId { get; private set; }
    public long BookingId { get; private set; }
    public long HoldReservationId { get; private set; }
    public long ResourceId { get; private set; }
    public int Quantity { get; private set; }
    public string? RequirementRoleCode { get; private set; }

    public static BookingResourceAllocation FromReservation(
        Booking booking,
        ResourceReservation reservation,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(reservation);
        if (booking.TenantId != reservation.TenantId
            || booking.BranchId != reservation.BranchId
            || booking.HoldId != reservation.HoldId)
            throw new DomainRuleException("Booking resource scope does not match its hold reservation.");

        var allocation = new BookingResourceAllocation
        {
            TenantId = booking.TenantId,
            BranchId = booking.BranchId,
            BookingId = booking.Id,
            HoldReservationId = reservation.Id,
            ResourceId = reservation.ResourceId,
            Quantity = reservation.Quantity,
            RequirementRoleCode = reservation.RequirementRoleCode
        };
        allocation.StampCreated(now);
        return allocation;
    }
}
