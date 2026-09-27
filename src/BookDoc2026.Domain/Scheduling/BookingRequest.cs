using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class BookingRequest : TenantScopedEntity
{
    private BookingRequest() { }

    public long BranchId { get; private set; }
    public long? PatientId { get; private set; }
    public string PatientFullName { get; private set; } = string.Empty;
    public string PatientPhone { get; private set; } = string.Empty;
    public string? PatientEmail { get; private set; }
    public long? PreferredPractitionerId { get; private set; }
    public long? ServiceId { get; private set; }
    public DateOnly PreferredDate { get; private set; }
    public string PreferredTimeSlot { get; private set; } = string.Empty;
    public string ReasonForVisit { get; private set; } = string.Empty;
    public BookingRequestStatus Status { get; private set; }
    public long? AssignedPractitionerId { get; private set; }
    public long? ConfirmedBookingId { get; private set; }
    public string? ReviewNotes { get; private set; }
    public DateTimeOffset? ReviewedUtc { get; private set; }
    public string? ReviewedByStaffId { get; private set; }
    public long Version { get; private set; } = 1;

    public static BookingRequest Submit(
        long tenantId,
        long branchId,
        long? patientId,
        string patientFullName,
        string patientPhone,
        string? patientEmail,
        long? preferredPractitionerId,
        long? serviceId,
        DateOnly preferredDate,
        string preferredTimeSlot,
        string reasonForVisit,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0)
            throw new DomainRuleException("Tenant and branch IDs are required.");

        var fullName = patientFullName?.Trim() ?? string.Empty;
        if (fullName.Length is < 2 or > 120)
            throw new DomainRuleException("Patient full name must be between 2 and 120 characters.");

        var phone = patientPhone?.Trim() ?? string.Empty;
        if (phone.Length is < 7 or > 25)
            throw new DomainRuleException("Patient phone number must be between 7 and 25 characters.");

        var timeSlot = preferredTimeSlot?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(timeSlot))
            throw new DomainRuleException("Preferred time slot is required.");

        var reason = reasonForVisit?.Trim() ?? string.Empty;
        if (reason.Length is < 3 or > 500)
            throw new DomainRuleException("Reason for visit must be between 3 and 500 characters.");

        var request = new BookingRequest
        {
            TenantId = tenantId,
            BranchId = branchId,
            PatientId = patientId,
            PatientFullName = fullName,
            PatientPhone = phone,
            PatientEmail = string.IsNullOrWhiteSpace(patientEmail) ? null : patientEmail.Trim(),
            PreferredPractitionerId = preferredPractitionerId,
            ServiceId = serviceId,
            PreferredDate = preferredDate,
            PreferredTimeSlot = timeSlot,
            ReasonForVisit = reason,
            Status = BookingRequestStatus.PendingApproval
        };

        request.StampCreated(now);
        return request;
    }

    public void MarkUnderReview(string staffId, DateTimeOffset now)
    {
        if (Status != BookingRequestStatus.PendingApproval)
            throw new DomainRuleException($"Cannot review request with status {Status}.");

        ReviewedByStaffId = staffId?.Trim();
        Status = BookingRequestStatus.UnderReview;
        ReviewedUtc = now;
        Version++;
        StampModified(now);
    }

    public void Approve(
        long? assignedPractitionerId,
        long? confirmedBookingId,
        string staffId,
        string? notes,
        DateTimeOffset now)
    {
        if (Status != BookingRequestStatus.PendingApproval && Status != BookingRequestStatus.UnderReview && Status != BookingRequestStatus.Rescheduled)
            throw new DomainRuleException($"Cannot approve request with status {Status}.");

        AssignedPractitionerId = assignedPractitionerId ?? PreferredPractitionerId;
        ConfirmedBookingId = confirmedBookingId;
        ReviewedByStaffId = staffId?.Trim();
        ReviewNotes = notes?.Trim();
        Status = BookingRequestStatus.Approved;
        ReviewedUtc = now;
        Version++;
        StampModified(now);
    }

    public void Decline(string staffId, string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainRuleException("A reason is required to decline a booking request.");

        if (Status != BookingRequestStatus.PendingApproval && Status != BookingRequestStatus.UnderReview)
            throw new DomainRuleException($"Cannot decline request with status {Status}.");

        ReviewedByStaffId = staffId?.Trim();
        ReviewNotes = reason.Trim();
        Status = BookingRequestStatus.Declined;
        ReviewedUtc = now;
        Version++;
        StampModified(now);
    }

    public void Reschedule(string offeredSlotNotes, string staffId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(offeredSlotNotes))
            throw new DomainRuleException("Offered slot details/notes are required to reschedule.");

        if (Status != BookingRequestStatus.PendingApproval && Status != BookingRequestStatus.UnderReview)
            throw new DomainRuleException($"Cannot reschedule request with status {Status}.");

        ReviewedByStaffId = staffId?.Trim();
        ReviewNotes = offeredSlotNotes.Trim();
        Status = BookingRequestStatus.Rescheduled;
        ReviewedUtc = now;
        Version++;
        StampModified(now);
    }
}
