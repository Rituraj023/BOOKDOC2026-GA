namespace BookDoc2026.Contracts.Scheduling;

public sealed record CreateAvailabilityRuleRequest(
    string ResourceId,
    string? ServiceId,
    string DayOfWeek,
    TimeOnly LocalStart,
    TimeOnly LocalEnd,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int SlotIntervalMinutes,
    int Capacity);

public sealed record AvailabilityRuleResponse(
    string Id, string ResourceId, string? ServiceId, string DayOfWeek,
    TimeOnly LocalStart, TimeOnly LocalEnd, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    int SlotIntervalMinutes, int Capacity, bool IsActive, long Version);

public sealed record CreateAvailabilityExceptionRequest(
    string ResourceId, DateTimeOffset StartUtc, DateTimeOffset EndUtc,
    string Kind, int? CapacityOverride, string Reason);

public sealed record AvailabilityExceptionResponse(
    string Id, string ResourceId, DateTimeOffset StartUtc, DateTimeOffset EndUtc,
    string Kind, int? CapacityOverride, string Reason);

public sealed record AvailabilityResourceResponse(
    string ResourceId, string ResourceCode, string ResourceName, int Capacity,
    int ReservedQuantity, int RemainingCapacity, bool IsAvailable, string? UnavailableReason);

public sealed record HoldResourceRequest(string ResourceId, int Quantity, string? RequirementRoleCode = null);

public sealed record CreateSchedulingHoldRequest(
    Guid RequestId,
    string PatientId,
    string ServiceId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int HoldMinutes,
    IReadOnlyCollection<HoldResourceRequest> Resources);

public sealed record HoldResourceResponse(string ResourceId, int Quantity, string? RequirementRoleCode);

public sealed record SchedulingHoldResponse(
    string Id, Guid RequestId, string PatientId, string ServiceId,
    DateTimeOffset StartUtc, DateTimeOffset EndUtc, DateTimeOffset ExpiresUtc,
    string Status, long Version, IReadOnlyCollection<HoldResourceResponse> Resources);

public sealed record ReleaseSchedulingHoldRequest(long ExpectedVersion);

public sealed record ConfirmSchedulingHoldRequest(long ExpectedVersion);

public sealed record CancelBookingRequest(long ExpectedVersion, string Reason);

public sealed record RescheduleBookingRequest(
    string ReplacementHoldId,
    long ExpectedBookingVersion,
    long ExpectedHoldVersion,
    string Reason);

public sealed record CreateBookingWaitlistRequest(
    string PatientId,
    string ServiceId,
    DateTimeOffset EarliestStartUtc,
    DateTimeOffset LatestStartUtc,
    int Priority,
    string Reason);

public sealed record WithdrawBookingWaitlistRequest(long ExpectedVersion, string Reason);

public sealed record PromoteBookingWaitlistRequest(string HoldId, long ExpectedWaitlistVersion, long ExpectedHoldVersion);

public sealed record BookingWaitlistResponse(
    string Id,
    string PatientId,
    string ServiceId,
    DateTimeOffset EarliestStartUtc,
    DateTimeOffset LatestStartUtc,
    int Priority,
    string Reason,
    string Status,
    string? PromotedBookingId,
    long Version);

public sealed record BookingResourceResponse(
    string ResourceId,
    int Quantity,
    string? RequirementRoleCode);

public sealed record BookingResponse(
    string Id,
    string BookingNumber,
    string HoldId,
    string PatientId,
    string ServiceId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Status,
    DateTimeOffset ConfirmedUtc,
    DateTimeOffset? CancelledUtc,
    string? CancellationReason,
    string? PreviousBookingId,
    string? ReplacedByBookingId,
    string? WaitlistEntryId,
    long Version,
    IReadOnlyCollection<BookingResourceResponse> Resources,
    bool NotificationQueued,
    bool IsReplay);
