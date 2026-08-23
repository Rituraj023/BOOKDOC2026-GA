using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Abstractions;

public sealed record SchedulingHoldAggregate(
    SchedulingHold Hold,
    IReadOnlyCollection<ResourceReservation> Reservations);

public sealed record HoldCreationResult(SchedulingHoldAggregate Aggregate, bool IsReplay);

public sealed record BookingAggregate(
    Booking Booking,
    IReadOnlyCollection<BookingResourceAllocation> Resources);

public sealed record BookingConfirmationResult(BookingAggregate Aggregate, bool IsReplay);

public sealed record WaitlistPromotionResult(
    BookingAggregate Booking,
    BookingWaitlistEntry Waitlist,
    bool IsReplay);

public interface ISchedulingRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<BookableResource?> GetResourceAsync(long branchId, long resourceId, CancellationToken cancellationToken);
    Task<bool> ServiceExistsAsync(long serviceId, CancellationToken cancellationToken);
    Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken);
    Task<long?> GetPatientStakeholderIdAsync(long patientId, CancellationToken cancellationToken);
    Task<bool> ResourceSupportsServiceAsync(long resourceId, long serviceId, CancellationToken cancellationToken);
    Task AddRuleAsync(AvailabilityRule rule, CancellationToken cancellationToken);
    Task AddExceptionAsync(AvailabilityException exception, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BookableResource>> ListServiceResourcesAsync(long branchId, long serviceId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AvailabilityRule>> ListRulesAsync(long branchId, IReadOnlyCollection<long> resourceIds, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AvailabilityException>> ListExceptionsAsync(long branchId, IReadOnlyCollection<long> resourceIds, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<long, int>> GetReservedQuantitiesAsync(IReadOnlyCollection<long> resourceIds, DateTimeOffset startUtc, DateTimeOffset endUtc, DateTimeOffset now, CancellationToken cancellationToken);
    Task<HoldCreationResult> CreateHoldAtomicallyAsync(SchedulingHold hold, IReadOnlyCollection<ResourceReservation> reservations, AuditEvent auditEvent, DateTimeOffset now, CancellationToken cancellationToken);
    Task<SchedulingHoldAggregate?> GetHoldAsync(long branchId, long holdId, CancellationToken cancellationToken);
    Task<BookingConfirmationResult> ConfirmHoldAtomicallyAsync(
        Booking booking,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        long expectedHoldVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<BookingAggregate?> GetBookingByHoldAsync(long branchId, long holdId, CancellationToken cancellationToken);
    Task<BookingAggregate?> GetBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken);
    Task<BookingAggregate> CancelBookingAtomicallyAsync(
        long branchId,
        long bookingId,
        long expectedVersion,
        string reason,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<BookingConfirmationResult> RescheduleBookingAtomicallyAsync(
        long originalBookingId,
        Booking replacement,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        long expectedBookingVersion,
        long expectedHoldVersion,
        string reason,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task AddWaitlistAsync(BookingWaitlistEntry entry, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<BookingWaitlistEntry?> GetWaitlistAsync(long branchId, long waitlistId, bool tracked, CancellationToken cancellationToken);
    Task<WaitlistPromotionResult> PromoteWaitlistAtomicallyAsync(
        long waitlistId,
        Booking booking,
        IReadOnlyCollection<BookingResourceAllocation> resources,
        long expectedWaitlistVersion,
        long expectedHoldVersion,
        AuditEvent auditEvent,
        OutboxMessage outboxMessage,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
