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

public sealed record ShiftBreakWindow(TimeOnly StartTime, TimeOnly EndTime, string? Description = null);

public sealed record GenerateDoctorSlotsRequest(
    string BranchId,
    string PractitionerId,
    string? ServiceId,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyCollection<DayOfWeek> DaysOfWeek,
    TimeOnly ShiftStart,
    TimeOnly ShiftEnd,
    int SlotDurationMinutes,
    int MaxCapacityPerSlot,
    IReadOnlyCollection<ShiftBreakWindow>? Breaks = null);

public sealed record DoctorSlotResponse(
    string Id,
    string BranchId,
    string PractitionerId,
    string? PractitionerName,
    string? ServiceId,
    string? ServiceName,
    DateOnly SlotDate,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int DurationMinutes,
    int MaxCapacity,
    int BookedCount,
    int AvailableCapacity,
    string Status,
    string? BlockReason,
    long Version);

public sealed record BlockDoctorSlotRequest(
    string Reason);

public sealed record BookSlotDirectRequest(
    string SlotId,
    string? PatientId,
    string PatientFullName,
    string PatientPhone,
    string? PatientEmail,
    string? Notes = null);

public sealed record DirectSlotBookingConfirmationResponse(
    string BookingId,
    string BookingNumber,
    string SlotId,
    string PractitionerName,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string PatientFullName,
    string Status,
    string TokenCode);

public sealed record SubmitBookingRequest(
    string BranchId,
    string? PatientId,
    string PatientFullName,
    string PatientPhone,
    string? PatientEmail,
    string? PreferredPractitionerId,
    string? ServiceId,
    DateOnly PreferredDate,
    string PreferredTimeSlot,
    string ReasonForVisit);

public sealed record BookingRequestResponse(
    string Id,
    string BranchId,
    string? PatientId,
    string PatientFullName,
    string PatientPhone,
    string? PatientEmail,
    string? PreferredPractitionerId,
    string? PreferredPractitionerName,
    string? ServiceId,
    string? ServiceName,
    DateOnly PreferredDate,
    string PreferredTimeSlot,
    string ReasonForVisit,
    string Status,
    string? AssignedPractitionerId,
    string? AssignedPractitionerName,
    string? ConfirmedBookingId,
    string? ReviewNotes,
    DateTimeOffset? ReviewedUtc,
    string? ReviewedByStaffId,
    DateTimeOffset CreatedUtc,
    long Version);

public sealed record ApproveBookingRequest(
    string? AssignedPractitionerId = null,
    string? ConfirmedBookingId = null,
    string? Notes = null);

public sealed record DeclineBookingRequest(
    string Reason);

public sealed record RescheduleBookingRequestNotice(
    string OfferedSlotNotes);
