namespace BookDoc2026.Application.Scheduling;

public sealed record BookingLifecycleOutboxPayload(
    long BookingId,
    long TenantId,
    long OrganizationId,
    long BranchId,
    long PatientId,
    long StakeholderId,
    long? PreviousBookingId = null)
{
    public const string PreferencePurpose = "BOOKING.UPDATES";
    public const string CancelledMessageType = "Scheduling.BookingCancelled.Patient.v1";
    public const string RescheduledMessageType = "Scheduling.BookingRescheduled.Patient.v1";
    public const string WaitlistPromotedMessageType = "Scheduling.WaitlistPromoted.Patient.v1";
    public const string CancelledTemplateKey = "Booking.Cancelled.Patient";
    public const string RescheduledTemplateKey = "Booking.Rescheduled.Patient";
    public const string WaitlistPromotedTemplateKey = "Booking.WaitlistPromoted.Patient";
}
