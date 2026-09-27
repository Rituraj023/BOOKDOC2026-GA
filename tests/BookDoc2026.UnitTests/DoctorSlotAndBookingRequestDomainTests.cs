using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Scheduling;
using Xunit;

namespace BookDoc2026.UnitTests;

public sealed class DoctorSlotAndBookingRequestDomainTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void BookingSlot_CanBeCreated_WithValidParameters()
    {
        var startUtc = Now.AddHours(1);
        var endUtc = startUtc.AddMinutes(15);

        var slot = BookingSlot.Create(
            tenantId: 100,
            branchId: 200,
            practitionerId: 300,
            serviceId: 400,
            slotDate: Today,
            startUtc: startUtc,
            endUtc: endUtc,
            durationMinutes: 15,
            maxCapacity: 2,
            now: Now);

        Assert.Equal(100, slot.TenantId);
        Assert.Equal(200, slot.BranchId);
        Assert.Equal(300, slot.PractitionerId);
        Assert.Equal(400, slot.ServiceId);
        Assert.Equal(BookingSlotStatus.Available, slot.Status);
        Assert.Equal(2, slot.MaxCapacity);
        Assert.Equal(0, slot.BookedCount);
    }

    [Fact]
    public void BookingSlot_Throws_WhenStartAfterEnd()
    {
        var startUtc = Now.AddHours(2);
        var endUtc = Now.AddHours(1);

        Assert.Throws<DomainRuleException>(() => BookingSlot.Create(
            100, 200, 300, null, Today, startUtc, endUtc, 15, 1, Now));
    }

    [Fact]
    public void BookingSlot_ReserveCapacity_TransitionsStatusCorrectly()
    {
        var startUtc = Now.AddHours(1);
        var endUtc = startUtc.AddMinutes(15);

        var slot = BookingSlot.Create(100, 200, 300, null, Today, startUtc, endUtc, 15, 2, Now);

        slot.ReserveCapacity(1, Now.AddMinutes(1));
        Assert.Equal(1, slot.BookedCount);
        Assert.Equal(BookingSlotStatus.PartiallyBooked, slot.Status);

        slot.ReserveCapacity(1, Now.AddMinutes(2));
        Assert.Equal(2, slot.BookedCount);
        Assert.Equal(BookingSlotStatus.FullyBooked, slot.Status);

        // Attempting to reserve when fully booked throws exception
        Assert.Throws<DomainRuleException>(() => slot.ReserveCapacity(1, Now.AddMinutes(3)));
    }

    [Fact]
    public void BookingSlot_ReleaseCapacity_RestoresAvailableStatus()
    {
        var startUtc = Now.AddHours(1);
        var endUtc = startUtc.AddMinutes(15);

        var slot = BookingSlot.Create(100, 200, 300, null, Today, startUtc, endUtc, 15, 1, Now);
        slot.ReserveCapacity(1, Now.AddMinutes(1));
        Assert.Equal(BookingSlotStatus.FullyBooked, slot.Status);

        slot.ReleaseCapacity(1, Now.AddMinutes(2));
        Assert.Equal(0, slot.BookedCount);
        Assert.Equal(BookingSlotStatus.Available, slot.Status);
    }

    [Fact]
    public void BookingSlot_BlockAndUnblock_ManagesStatusAccurately()
    {
        var startUtc = Now.AddHours(1);
        var endUtc = startUtc.AddMinutes(15);

        var slot = BookingSlot.Create(100, 200, 300, null, Today, startUtc, endUtc, 15, 1, Now);
        slot.Block("Doctor attending emergency surgery", Now.AddMinutes(1));

        Assert.Equal(BookingSlotStatus.Blocked, slot.Status);
        Assert.Equal("Doctor attending emergency surgery", slot.BlockReason);

        slot.Unblock(Now.AddMinutes(2));
        Assert.Equal(BookingSlotStatus.Available, slot.Status);
        Assert.Null(slot.BlockReason);
    }

    [Fact]
    public void BookingRequest_Submit_CreatesPendingApprovalRequest()
    {
        var req = BookingRequest.Submit(
            tenantId: 100,
            branchId: 200,
            patientId: null,
            patientFullName: "Suresh Gupta",
            patientPhone: "+919876543210",
            patientEmail: "suresh@example.com",
            preferredPractitionerId: 300,
            serviceId: null,
            preferredDate: Today.AddDays(2),
            preferredTimeSlot: "Morning (09:00 AM - 12:00 PM)",
            reasonForVisit: "Persistent backache for 2 weeks",
            now: Now);

        Assert.Equal("Suresh Gupta", req.PatientFullName);
        Assert.Equal("+919876543210", req.PatientPhone);
        Assert.Equal(BookingRequestStatus.PendingApproval, req.Status);
        Assert.Equal(300, req.PreferredPractitionerId);
    }

    [Fact]
    public void BookingRequest_Throws_WhenInvalidPatientPhone()
    {
        Assert.Throws<DomainRuleException>(() => BookingRequest.Submit(
            100, 200, null, "Suresh Gupta", "12", null, null, null, Today, "Morning", "Fever", Now));
    }

    [Fact]
    public void BookingRequest_Approve_UpdatesStatusAndStaffInfo()
    {
        var req = BookingRequest.Submit(
            100, 200, null, "Priya Sharma", "+919876543210", null, null, null, Today, "Evening", "Routine checkup", Now);

        req.Approve(assignedPractitionerId: 305, confirmedBookingId: null, staffId: "Staff01", notes: "Approved for room 102", now: Now.AddHours(1));

        Assert.Equal(BookingRequestStatus.Approved, req.Status);
        Assert.Equal(305, req.AssignedPractitionerId);
        Assert.Equal("Staff01", req.ReviewedByStaffId);
        Assert.Equal("Approved for room 102", req.ReviewNotes);
    }

    [Fact]
    public void BookingRequest_Decline_SetsDeclinedStatusWithReason()
    {
        var req = BookingRequest.Submit(
            100, 200, null, "Amit Verma", "+919876543210", null, null, null, Today, "Morning", "Consultation", Now);

        req.Decline("Staff02", "Specialist doctor is traveling overseas", Now.AddHours(1));

        Assert.Equal(BookingRequestStatus.Declined, req.Status);
        Assert.Equal("Specialist doctor is traveling overseas", req.ReviewNotes);
        Assert.Equal("Staff02", req.ReviewedByStaffId);
    }

    [Fact]
    public void BookingRequest_Reschedule_SetsRescheduledStatus()
    {
        var req = BookingRequest.Submit(
            100, 200, null, "Geeta Bai", "+919876543210", null, null, null, Today, "Morning", "Eye checkup", Now);

        req.Reschedule("Offered slot: Friday at 11:00 AM", "Staff03", Now.AddHours(1));

        Assert.Equal(BookingRequestStatus.Rescheduled, req.Status);
        Assert.Equal("Offered slot: Friday at 11:00 AM", req.ReviewNotes);
    }
}
