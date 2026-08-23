namespace BookDoc2026.Application.Scheduling;

public sealed record BookingConfirmedOutboxPayload(
    long BookingId,
    long TenantId,
    long OrganizationId,
    long BranchId,
    long PatientId,
    long StakeholderId)
{
    public const string MessageType = "Scheduling.BookingConfirmed.Patient.v1";
    public const string PreferencePurpose = "BOOKING.CONFIRMATION";
    public const string TemplateKey = "Booking.Confirmed.Patient";
}
