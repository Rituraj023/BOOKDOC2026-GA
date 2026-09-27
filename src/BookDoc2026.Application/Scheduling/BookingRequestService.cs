using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Scheduling;

public sealed class BookingRequestService(
    ISchedulingRepository repository,
    IPractitionerRepository practitionerRepository,
    ICatalogRepository catalogRepository,
    IPublicIdCodec codec,
    ICurrentActor currentActor,
    IClock clock) : IBookingRequestService
{
    public async Task<BookingRequestResponse> SubmitRequestAsync(
        SubmitBookingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = EnsureTenantId();

        var branchId = codec.Decode(PublicIdKind.Branch, request.BranchId, tenantId);
        var patientId = codec.DecodeOptional(PublicIdKind.Patient, request.PatientId, tenantId);
        var preferredPractitionerId = codec.DecodeOptional(PublicIdKind.Practitioner, request.PreferredPractitionerId, tenantId);
        var serviceId = codec.DecodeOptional(PublicIdKind.ClinicalService, request.ServiceId, tenantId);

        var bookingRequest = BookingRequest.Submit(
            tenantId,
            branchId,
            patientId,
            request.PatientFullName,
            request.PatientPhone,
            request.PatientEmail,
            preferredPractitionerId,
            serviceId,
            request.PreferredDate,
            request.PreferredTimeSlot,
            request.ReasonForVisit,
            clock.UtcNow);

        await repository.AddBookingRequestAsync(bookingRequest, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var responses = await MapRequestsToResponsesAsync(branchId, new List<BookingRequest> { bookingRequest }, tenantId, cancellationToken);
        return responses.First();
    }

    public async Task<IReadOnlyCollection<BookingRequestResponse>> GetRequestsAsync(
        string branchId,
        string? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);

        BookingRequestStatus? filterStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingRequestStatus>(status, true, out var parsedStatus))
        {
            filterStatus = parsedStatus;
        }

        var requests = await repository.ListBookingRequestsAsync(
            branchInternalId,
            filterStatus,
            fromDate,
            toDate,
            cancellationToken);

        return await MapRequestsToResponsesAsync(branchInternalId, requests, tenantId, cancellationToken);
    }

    public async Task<BookingRequestResponse> GetRequestByIdAsync(
        string branchId,
        string requestId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var requestInternalId = codec.Decode(PublicIdKind.BookingRequest, requestId, tenantId);

        var request = await repository.GetBookingRequestAsync(branchInternalId, requestInternalId, tracked: false, cancellationToken)
            ?? throw new NotFoundException("Booking request not found.");

        var responses = await MapRequestsToResponsesAsync(branchInternalId, new List<BookingRequest> { request }, tenantId, cancellationToken);
        return responses.First();
    }

    public async Task<BookingRequestResponse> ApproveRequestAsync(
        string branchId,
        string requestId,
        ApproveBookingRequest command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var requestInternalId = codec.Decode(PublicIdKind.BookingRequest, requestId, tenantId);

        var request = await repository.GetBookingRequestAsync(branchInternalId, requestInternalId, tracked: true, cancellationToken)
            ?? throw new NotFoundException("Booking request not found.");

        var assignedPractitionerId = codec.DecodeOptional(PublicIdKind.Practitioner, command.AssignedPractitionerId, tenantId);
        var confirmedBookingId = codec.DecodeOptional(PublicIdKind.Booking, command.ConfirmedBookingId, tenantId);

        request.Approve(
            assignedPractitionerId,
            confirmedBookingId,
            currentActor.ActorId.ToString(),
            command.Notes,
            clock.UtcNow);

        await repository.SaveChangesAsync(cancellationToken);

        var responses = await MapRequestsToResponsesAsync(branchInternalId, new List<BookingRequest> { request }, tenantId, cancellationToken);
        return responses.First();
    }

    public async Task<BookingRequestResponse> DeclineRequestAsync(
        string branchId,
        string requestId,
        DeclineBookingRequest command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var requestInternalId = codec.Decode(PublicIdKind.BookingRequest, requestId, tenantId);

        var request = await repository.GetBookingRequestAsync(branchInternalId, requestInternalId, tracked: true, cancellationToken)
            ?? throw new NotFoundException("Booking request not found.");

        request.Decline(
            currentActor.ActorId.ToString(),
            command.Reason,
            clock.UtcNow);

        await repository.SaveChangesAsync(cancellationToken);

        var responses = await MapRequestsToResponsesAsync(branchInternalId, new List<BookingRequest> { request }, tenantId, cancellationToken);
        return responses.First();
    }

    public async Task<BookingRequestResponse> RescheduleRequestAsync(
        string branchId,
        string requestId,
        RescheduleBookingRequestNotice command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var requestInternalId = codec.Decode(PublicIdKind.BookingRequest, requestId, tenantId);

        var request = await repository.GetBookingRequestAsync(branchInternalId, requestInternalId, tracked: true, cancellationToken)
            ?? throw new NotFoundException("Booking request not found.");

        request.Reschedule(
            command.OfferedSlotNotes,
            currentActor.ActorId.ToString(),
            clock.UtcNow);

        await repository.SaveChangesAsync(cancellationToken);

        var responses = await MapRequestsToResponsesAsync(branchInternalId, new List<BookingRequest> { request }, tenantId, cancellationToken);
        return responses.First();
    }

    private async Task<IReadOnlyCollection<BookingRequestResponse>> MapRequestsToResponsesAsync(
        long branchId,
        IEnumerable<BookingRequest> requests,
        long tenantId,
        CancellationToken cancellationToken)
    {
        var roster = await practitionerRepository.ListBranchPractitionersAsync(branchId, cancellationToken);
        var practitionerMap = roster.ToDictionary(r => r.Profile.Id, r => r.DisplayName);

        var responses = new List<BookingRequestResponse>();
        foreach (var req in requests)
        {
            string? preferredDocName = null;
            if (req.PreferredPractitionerId.HasValue && practitionerMap.TryGetValue(req.PreferredPractitionerId.Value, out var pName))
            {
                preferredDocName = pName;
            }

            string? assignedDocName = null;
            if (req.AssignedPractitionerId.HasValue && practitionerMap.TryGetValue(req.AssignedPractitionerId.Value, out var aName))
            {
                assignedDocName = aName;
            }

            string? serviceName = null;
            if (req.ServiceId.HasValue)
            {
                var svc = await catalogRepository.GetServiceAsync(req.ServiceId.Value, cancellationToken);
                serviceName = svc?.Name;
            }

            responses.Add(new BookingRequestResponse(
                Id: codec.Encode(PublicIdKind.BookingRequest, req.Id, tenantId),
                BranchId: codec.Encode(PublicIdKind.Branch, req.BranchId, tenantId),
                PatientId: codec.EncodeOptional(PublicIdKind.Patient, req.PatientId, tenantId),
                PatientFullName: req.PatientFullName,
                PatientPhone: req.PatientPhone,
                PatientEmail: req.PatientEmail,
                PreferredPractitionerId: codec.EncodeOptional(PublicIdKind.Practitioner, req.PreferredPractitionerId, tenantId),
                PreferredPractitionerName: preferredDocName,
                ServiceId: codec.EncodeOptional(PublicIdKind.ClinicalService, req.ServiceId, tenantId),
                ServiceName: serviceName,
                PreferredDate: req.PreferredDate,
                PreferredTimeSlot: req.PreferredTimeSlot,
                ReasonForVisit: req.ReasonForVisit,
                Status: req.Status.ToString(),
                AssignedPractitionerId: codec.EncodeOptional(PublicIdKind.Practitioner, req.AssignedPractitionerId, tenantId),
                AssignedPractitionerName: assignedDocName,
                ConfirmedBookingId: codec.EncodeOptional(PublicIdKind.Booking, req.ConfirmedBookingId, tenantId),
                ReviewNotes: req.ReviewNotes,
                ReviewedUtc: req.ReviewedUtc,
                ReviewedByStaffId: req.ReviewedByStaffId,
                CreatedUtc: req.CreatedUtc,
                Version: req.Version));
        }

        return responses;
    }

    private long EnsureTenantId()
    {
        if (!currentActor.TenantId.HasValue || currentActor.TenantId.Value <= 0)
            throw new DomainRuleException("A valid tenant context is required.");
        return currentActor.TenantId.Value;
    }
}
