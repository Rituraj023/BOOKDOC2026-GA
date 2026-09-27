using System.Net.Http.Json;
using BookDoc2026.Contracts.Communications;
using BookDoc2026.Contracts.Billing;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Contracts;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Queues;
using BookDoc2026.Contracts.Radiology;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Workforce;

namespace BookDoc2026.Client;

public sealed class BookDocApiClient(HttpClient httpClient)
{
    public Task<IReadOnlyCollection<PhysiotherapyCarePlanListItemResponse>> ListPhysiotherapyCarePlansAsync(
        string branchId, int take = 50, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<PhysiotherapyCarePlanListItemResponse>>(
            $"{PhysiotherapyUrl(branchId)}?take={Math.Clamp(take, 1, 100)}", cancellationToken);

    public Task<PhysiotherapyCarePlanResponse> CreatePhysiotherapyCarePlanAsync(string branchId,
        CreatePhysiotherapyCarePlanRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<CreatePhysiotherapyCarePlanRequest, PhysiotherapyCarePlanResponse>(
            PhysiotherapyUrl(branchId), request, cancellationToken);

    public Task<PhysiotherapyCarePlanResponse> GetPhysiotherapyCarePlanAsync(string branchId, string carePlanId,
        CancellationToken cancellationToken = default) => GetAsync<PhysiotherapyCarePlanResponse>(
        PhysiotherapyUrl(branchId, carePlanId), cancellationToken);

    public Task<PhysiotherapyCarePlanResponse> RevisePhysiotherapyCarePlanAsync(string branchId, string carePlanId,
        RevisePhysiotherapyCarePlanRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RevisePhysiotherapyCarePlanRequest, PhysiotherapyCarePlanResponse>(
            $"{PhysiotherapyUrl(branchId, carePlanId)}/revisions", request, cancellationToken);

    public Task<PhysiotherapyCarePlanResponse> ActivatePhysiotherapyCarePlanAsync(string branchId,
        string carePlanId, ActivatePhysiotherapyCarePlanRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<ActivatePhysiotherapyCarePlanRequest, PhysiotherapyCarePlanResponse>(
            $"{PhysiotherapyUrl(branchId, carePlanId)}/activate", request, cancellationToken);

    public Task<PhysiotherapyCarePlanResponse> ClosePhysiotherapyCarePlanAsync(string branchId,
        string carePlanId, ChangePhysiotherapyCarePlanStatusRequest request, bool discontinued,
        CancellationToken cancellationToken = default) =>
        PostAsync<ChangePhysiotherapyCarePlanStatusRequest, PhysiotherapyCarePlanResponse>(
            $"{PhysiotherapyUrl(branchId, carePlanId)}/{(discontinued ? "discontinue" : "complete")}",
            request, cancellationToken);

    public Task<PhysiotherapyCarePlanResponse> RecordPhysiotherapySessionAsync(string branchId,
        string carePlanId, RecordPhysiotherapySessionRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<RecordPhysiotherapySessionRequest, PhysiotherapyCarePlanResponse>(
            $"{PhysiotherapyUrl(branchId, carePlanId)}/sessions", request, cancellationToken);

    public Task<PhysiotherapyCarePlanResponse> RecordPhysiotherapyOutcomeAsync(string branchId,
        string carePlanId, RecordPhysiotherapyOutcomeRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<RecordPhysiotherapyOutcomeRequest, PhysiotherapyCarePlanResponse>(
            $"{PhysiotherapyUrl(branchId, carePlanId)}/outcomes", request, cancellationToken);

    public Task<IReadOnlyCollection<InvoiceResponse>> ListInvoicesAsync(string branchId, int take = 50,
        CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<InvoiceResponse>>(
        $"{BillingUrl(branchId, "invoices")}?take={take}", cancellationToken);

    public Task<InvoiceResponse> IssueInvoiceAsync(string branchId, IssueInvoiceRequest request,
        CancellationToken cancellationToken = default) => PostAsync<IssueInvoiceRequest, InvoiceResponse>(
        BillingUrl(branchId, "invoices"), request, cancellationToken);

    public Task<InvoiceResponse> GetInvoiceAsync(string branchId, string invoiceId,
        CancellationToken cancellationToken = default) => GetAsync<InvoiceResponse>(
        BillingUrl(branchId, $"invoices/{Uri.EscapeDataString(invoiceId)}"), cancellationToken);

    public Task<PaymentResponse> ReceivePaymentAsync(string branchId, ReceivePaymentRequest request,
        CancellationToken cancellationToken = default) => PostAsync<ReceivePaymentRequest, PaymentResponse>(
        BillingUrl(branchId, "payments"), request, cancellationToken);

    public Task<IReadOnlyCollection<PaymentResponse>> ListPaymentsAsync(string branchId, int take = 50,
        CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<PaymentResponse>>(
        $"{BillingUrl(branchId, "payments")}?take={take}", cancellationToken);

    public Task<PaymentResponse> GetPaymentAsync(string branchId, string paymentId,
        CancellationToken cancellationToken = default) => GetAsync<PaymentResponse>(
        BillingUrl(branchId, $"payments/{Uri.EscapeDataString(paymentId)}"), cancellationToken);

    public Task<PaymentResponse> AllocatePaymentAsync(string branchId, string paymentId,
        AllocatePaymentRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<AllocatePaymentRequest, PaymentResponse>(
            BillingUrl(branchId, $"payments/{Uri.EscapeDataString(paymentId)}/allocations"), request,
            cancellationToken);

    public Task<PractitionerResponse> CreatePractitionerAsync(string branchId, CreatePractitionerRequest request,
        CancellationToken cancellationToken = default) => PostAsync<CreatePractitionerRequest, PractitionerResponse>(
        PractitionerUrl(branchId), request, cancellationToken);

    public Task<PractitionerResponse> GetPractitionerAsync(string branchId, string practitionerId,
        CancellationToken cancellationToken = default) => GetAsync<PractitionerResponse>(
        PractitionerUrl(branchId, practitionerId), cancellationToken);

    public Task<PractitionerResponse> AddPractitionerCredentialAsync(string branchId, string practitionerId,
        AddPractitionerCredentialRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<AddPractitionerCredentialRequest, PractitionerResponse>(
            $"{PractitionerUrl(branchId, practitionerId)}/credentials", request, cancellationToken);

    public Task<PractitionerResponse> VerifyPractitionerCredentialAsync(string branchId, string practitionerId,
        string credentialId, DecidePractitionerCredentialRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<DecidePractitionerCredentialRequest, PractitionerResponse>(
            $"{PractitionerUrl(branchId, practitionerId)}/credentials/{Uri.EscapeDataString(credentialId)}/verify",
            request, cancellationToken);

    public Task<PractitionerResponse> RejectPractitionerCredentialAsync(string branchId, string practitionerId,
        string credentialId, DecidePractitionerCredentialRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<DecidePractitionerCredentialRequest, PractitionerResponse>(
            $"{PractitionerUrl(branchId, practitionerId)}/credentials/{Uri.EscapeDataString(credentialId)}/reject",
            request, cancellationToken);

    public Task<PractitionerResponse> AddPractitionerAssignmentAsync(string branchId, string practitionerId,
        AddPractitionerAssignmentRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<AddPractitionerAssignmentRequest, PractitionerResponse>(
            $"{PractitionerUrl(branchId, practitionerId)}/assignments", request, cancellationToken);

    public Task<PractitionerResponse> ActivatePractitionerAsync(string branchId, string practitionerId,
        long expectedVersion, CancellationToken cancellationToken = default) =>
        PostAsync<ChangePractitionerStatusRequest, PractitionerResponse>(
            $"{PractitionerUrl(branchId, practitionerId)}/activate",
            new ChangePractitionerStatusRequest(expectedVersion), cancellationToken);

    public Task<PractitionerResponse> SuspendPractitionerAsync(string branchId, string practitionerId,
        long expectedVersion, CancellationToken cancellationToken = default) => PractitionerStatusAsync(
            branchId, practitionerId, "suspend", expectedVersion, cancellationToken);

    public Task<PractitionerResponse> DeactivatePractitionerAsync(string branchId, string practitionerId,
        long expectedVersion, CancellationToken cancellationToken = default) => PractitionerStatusAsync(
            branchId, practitionerId, "deactivate", expectedVersion, cancellationToken);

    public Task<PractitionerResponse> SuspendPractitionerAssignmentAsync(string branchId, string practitionerId,
        string assignmentId, long expectedVersion, CancellationToken cancellationToken = default) =>
        PractitionerAssignmentStatusAsync(branchId, practitionerId, assignmentId, "suspend", expectedVersion,
            cancellationToken);

    public Task<PractitionerResponse> EndPractitionerAssignmentAsync(string branchId, string practitionerId,
        string assignmentId, long expectedVersion, CancellationToken cancellationToken = default) =>
        PractitionerAssignmentStatusAsync(branchId, practitionerId, assignmentId, "end", expectedVersion,
            cancellationToken);

    public Task<IReadOnlyCollection<PractitionerSummaryResponse>> ListPractitionersAsync(
        string branchId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<PractitionerSummaryResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/practitioners", cancellationToken);

    public Task<EncounterResponse> StartEncounterAsync(
        string branchId, StartEncounterRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<StartEncounterRequest, EncounterResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters", request, cancellationToken);

    public Task<IReadOnlyCollection<ClinicalAgendaItemResponse>> ListClinicalAgendaAsync(string branchId,
        DateOnly date, int take = 50, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<ClinicalAgendaItemResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/agenda" +
            $"?date={date:yyyy-MM-dd}&take={Math.Clamp(take, 1, 100)}", cancellationToken);

    public Task<EncounterResponse> GetEncounterAsync(
        string branchId, string encounterId, CancellationToken cancellationToken = default) =>
        GetAsync<EncounterResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/{Uri.EscapeDataString(encounterId)}",
            cancellationToken);

    public Task<EncounterResponse> ReviseEncounterDraftAsync(
        string branchId, string encounterId, ReviseEncounterDraftRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<ReviseEncounterDraftRequest, EncounterResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/{Uri.EscapeDataString(encounterId)}/draft-revisions",
            request, cancellationToken);

    public Task<EncounterResponse> SignEncounterAsync(
        string branchId, string encounterId, SignEncounterRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<SignEncounterRequest, EncounterResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/{Uri.EscapeDataString(encounterId)}/sign",
            request, cancellationToken);

    public Task<EncounterResponse> AmendEncounterAsync(
        string branchId, string encounterId, AmendEncounterRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<AmendEncounterRequest, EncounterResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/{Uri.EscapeDataString(encounterId)}/amendments",
            request, cancellationToken);

    public Task<IReadOnlyCollection<InvestigationOrderResponse>> ListInvestigationOrdersAsync(
        string branchId, string encounterId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<InvestigationOrderResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/{Uri.EscapeDataString(encounterId)}/investigation-orders",
            cancellationToken);

    public Task<IReadOnlyCollection<InvestigationServiceOptionResponse>> ListInvestigationCatalogOptionsAsync(
        string branchId, string encounterId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<InvestigationServiceOptionResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/{Uri.EscapeDataString(encounterId)}/investigation-orders/catalog-options",
            cancellationToken);

    public Task<InvestigationOrderResponse> CreateInvestigationOrderAsync(
        string branchId, string encounterId, CreateInvestigationOrderRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreateInvestigationOrderRequest, InvestigationOrderResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/{Uri.EscapeDataString(encounterId)}/investigation-orders",
            request, cancellationToken);

    public Task<InvestigationOrderResponse> HandoffInvestigationOrderAsync(
        string branchId, string encounterId, string orderId, HandoffInvestigationOrderRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<HandoffInvestigationOrderRequest, InvestigationOrderResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/encounters/{Uri.EscapeDataString(encounterId)}/investigation-orders/{Uri.EscapeDataString(orderId)}/queue-handoffs",
            request, cancellationToken);

    public Task<ContractResponse> CreateContractAsync(
        string branchId, CreateContractRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<CreateContractRequest, ContractResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts", request, cancellationToken);

    public Task<ContractResponse> GetContractAsync(
        string branchId, string contractId, CancellationToken cancellationToken = default) =>
        GetAsync<ContractResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts/{Uri.EscapeDataString(contractId)}",
            cancellationToken);

    public Task<EntitlementReservationResponse> ReserveContractEntitlementAsync(
        string branchId, string contractId, string entitlementId, ReserveEntitlementRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<ReserveEntitlementRequest, EntitlementReservationResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts/{Uri.EscapeDataString(contractId)}/entitlements/{Uri.EscapeDataString(entitlementId)}/reservations",
            request, cancellationToken);

    public Task<EntitlementReservationResponse> ConsumeContractEntitlementAsync(
        string branchId, string reservationId, ConsumeEntitlementReservationRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<ConsumeEntitlementReservationRequest, EntitlementReservationResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts/entitlement-reservations/{Uri.EscapeDataString(reservationId)}/consume",
            request, cancellationToken);

    public Task<EntitlementReservationResponse> ReleaseContractEntitlementAsync(
        string branchId, string reservationId, ReleaseEntitlementReservationRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<ReleaseEntitlementReservationRequest, EntitlementReservationResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts/entitlement-reservations/{Uri.EscapeDataString(reservationId)}/release",
            request, cancellationToken);

    public Task<IReadOnlyCollection<PatientSearchResponse>> SearchPatientsAsync(
        string branchId,
        string query,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<PatientSearchResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients?q={Uri.EscapeDataString(query)}",
            cancellationToken);

    public Task<PatientResponse> RegisterPatientAsync(
        string branchId,
        RegisterPatientRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<RegisterPatientRequest, PatientResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients",
            request, cancellationToken);

    public Task<PatientResponse> GetPatientAsync(
        string branchId,
        string patientId,
        CancellationToken cancellationToken = default) =>
        GetAsync<PatientResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients/{Uri.EscapeDataString(patientId)}",
            cancellationToken);

    public Task<IReadOnlyCollection<ServiceResponse>> ListServicesAsync(
        string branchId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<ServiceResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/catalog/services?includeInactive={includeInactive.ToString().ToLowerInvariant()}",
            cancellationToken);

    public Task<IReadOnlyCollection<BookableResourceResponse>> ListResourcesAsync(
        string branchId,
        string? categoryId = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/branches/{Uri.EscapeDataString(branchId)}/resources?includeInactive={includeInactive.ToString().ToLowerInvariant()}";
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            url += $"&categoryId={Uri.EscapeDataString(categoryId)}";
        }
        return GetAsync<IReadOnlyCollection<BookableResourceResponse>>(url, cancellationToken);
    }

    public Task<IReadOnlyCollection<ImagingServicePointResponse>> ListImagingServicePointsAsync(
        string branchId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<ImagingServicePointResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/queues/imaging-service-points",
            cancellationToken);

    public Task<ImagingServicePointResponse> CreateImagingServicePointAsync(
        string branchId,
        CreateImagingServicePointRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreateImagingServicePointRequest, ImagingServicePointResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/queues/imaging-service-points",
            request, cancellationToken);

    public Task<QueueTicketResponse> CheckInQueueTicketAsync(
        string branchId,
        CheckInQueueTicketRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CheckInQueueTicketRequest, QueueTicketResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/queues/tickets",
            request, cancellationToken);

    public Task<IReadOnlyCollection<QueueTicketResponse>> ListQueueTicketsAsync(
        string branchId,
        string servicePointId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<QueueTicketResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/queues/imaging-service-points/{Uri.EscapeDataString(servicePointId)}/tickets",
            cancellationToken);

    public Task<IReadOnlyCollection<InvestigationWorklistItemResponse>> ListInvestigationWorklistAsync(
        string branchId,
        string servicePointId,
        int take = 100,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<InvestigationWorklistItemResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/investigations/worklist?servicePointId={Uri.EscapeDataString(servicePointId)}&take={take}",
            cancellationToken);

    public Task<RadiologyStudyResponse> GetRadiologyStudyByOrderAsync(string branchId, string orderId,
        CancellationToken cancellationToken = default) => GetAsync<RadiologyStudyResponse>(
        RadiologyOrderStudyUrl(branchId, orderId), cancellationToken);

    public Task<RadiologyStudyResponse> GetRadiologyStudyAsync(string branchId, string studyId,
        CancellationToken cancellationToken = default) => GetAsync<RadiologyStudyResponse>(
        RadiologyStudyUrl(branchId, studyId), cancellationToken);

    public Task<RadiologyStudyResponse> RegisterRadiologyStudyAsync(string branchId, string orderId,
        RegisterRadiologyStudyRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RegisterRadiologyStudyRequest, RadiologyStudyResponse>(
            RadiologyOrderStudyUrl(branchId, orderId), request, cancellationToken);

    public Task<RadiologyStudyResponse> StartRadiologyStudyAsync(string branchId, string studyId,
        StartRadiologyStudyRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<StartRadiologyStudyRequest, RadiologyStudyResponse>(
            $"{RadiologyStudyUrl(branchId, studyId)}/start", request, cancellationToken);

    public Task<IReadOnlyCollection<RadiologyEquipmentOptionResponse>> ListEligibleRadiologyEquipmentAsync(
        string branchId, string studyId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<RadiologyEquipmentOptionResponse>>(
            $"{RadiologyStudyUrl(branchId, studyId)}/eligible-equipment", cancellationToken);

    public Task<RadiologyStudyResponse> RecordRadiologyAcquisitionAsync(string branchId, string studyId,
        RecordRadiologyAcquisitionRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RecordRadiologyAcquisitionRequest, RadiologyStudyResponse>(
            $"{RadiologyStudyUrl(branchId, studyId)}/acquisitions", request, cancellationToken);

    public Task<RadiologyStudyResponse> ReviewRadiologyQualityAsync(string branchId, string studyId,
        ReviewRadiologyQualityRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ReviewRadiologyQualityRequest, RadiologyStudyResponse>(
            $"{RadiologyStudyUrl(branchId, studyId)}/quality-reviews", request, cancellationToken);

    public Task<IReadOnlyCollection<QueueDisplayTicketResponse>> GetQueueDisplayAsync(
        string branchId,
        string servicePointId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<QueueDisplayTicketResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/queues/imaging-service-points/{Uri.EscapeDataString(servicePointId)}/display",
            cancellationToken);

    public Task<QueueTicketResponse> TransitionQueueTicketAsync(
        string branchId,
        string ticketId,
        string action,
        QueueTransitionRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<QueueTransitionRequest, QueueTicketResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/queues/tickets/{Uri.EscapeDataString(ticketId)}/{Uri.EscapeDataString(action)}",
            request, cancellationToken);

    public Task<BookingResponse> ConfirmSchedulingHoldAsync(
        string branchId,
        string holdId,
        long expectedVersion,
        CancellationToken cancellationToken = default) =>
        PostAsync<ConfirmSchedulingHoldRequest, BookingResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/scheduling/holds/{Uri.EscapeDataString(holdId)}/confirm",
            new ConfirmSchedulingHoldRequest(expectedVersion),
            cancellationToken);

    public Task<BookingResponse> GetBookingAsync(
        string branchId,
        string bookingId,
        CancellationToken cancellationToken = default) =>
        GetAsync<BookingResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/scheduling/bookings/{Uri.EscapeDataString(bookingId)}",
            cancellationToken);

    public Task<BookingResponse> CancelBookingAsync(
        string branchId,
        string bookingId,
        CancelBookingRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CancelBookingRequest, BookingResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/scheduling/bookings/{Uri.EscapeDataString(bookingId)}/cancel",
            request,
            cancellationToken);

    public Task<BookingResponse> RescheduleBookingAsync(
        string branchId,
        string bookingId,
        RescheduleBookingRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<RescheduleBookingRequest, BookingResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/scheduling/bookings/{Uri.EscapeDataString(bookingId)}/reschedule",
            request,
            cancellationToken);

    public Task<BookingWaitlistResponse> CreateBookingWaitlistAsync(
        string branchId,
        CreateBookingWaitlistRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreateBookingWaitlistRequest, BookingWaitlistResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/scheduling/waitlist",
            request,
            cancellationToken);

    public Task<BookingWaitlistResponse> GetBookingWaitlistAsync(
        string branchId,
        string waitlistId,
        CancellationToken cancellationToken = default) =>
        GetAsync<BookingWaitlistResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/scheduling/waitlist/{Uri.EscapeDataString(waitlistId)}",
            cancellationToken);

    public Task<BookingWaitlistResponse> WithdrawBookingWaitlistAsync(
        string branchId,
        string waitlistId,
        WithdrawBookingWaitlistRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<WithdrawBookingWaitlistRequest, BookingWaitlistResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/scheduling/waitlist/{Uri.EscapeDataString(waitlistId)}/withdraw",
            request,
            cancellationToken);

    public Task<BookingResponse> PromoteBookingWaitlistAsync(
        string branchId,
        string waitlistId,
        PromoteBookingWaitlistRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<PromoteBookingWaitlistRequest, BookingResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/scheduling/waitlist/{Uri.EscapeDataString(waitlistId)}/promote",
            request,
            cancellationToken);

    public Task<IReadOnlyCollection<MessageTemplateResponse>> ListMessageTemplatesAsync(
        string branchId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<MessageTemplateResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/templates",
            cancellationToken);

    public Task<IReadOnlyCollection<CommunicationPreferenceResponse>> ListCommunicationPreferencesAsync(
        string branchId,
        string stakeholderId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<CommunicationPreferenceResponse>>(
            PreferenceUrl(branchId, stakeholderId), cancellationToken);

    public Task<CommunicationPreferenceResponse> RecordCommunicationPreferenceAsync(
        string branchId,
        string stakeholderId,
        RecordCommunicationPreferenceRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<RecordCommunicationPreferenceRequest, CommunicationPreferenceResponse>(
            PreferenceUrl(branchId, stakeholderId), request, cancellationToken);

    public Task<IReadOnlyCollection<ProviderCallbackInboxResponse>> ListProviderCallbacksAsync(
        string branchId,
        int take = 50,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<ProviderCallbackInboxResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/provider-callbacks?take={take}",
            cancellationToken);

    public Task<MessageTemplateResponse> CreateMessageTemplateVersionAsync(
        string branchId,
        CreateMessageTemplateVersionRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreateMessageTemplateVersionRequest, MessageTemplateResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/templates/versions",
            request,
            cancellationToken);

    public Task<MessageTemplatePreviewResponse> PreviewMessageTemplateAsync(
        string branchId,
        string templateId,
        PreviewMessageTemplateRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<PreviewMessageTemplateRequest, MessageTemplatePreviewResponse>(
            TemplateActionUrl(branchId, templateId, "preview"),
            request,
            cancellationToken);

    public Task<MessageTemplateResponse> PublishMessageTemplateAsync(
        string branchId,
        string templateId,
        long expectedRevision,
        CancellationToken cancellationToken = default) =>
        PostAsync<ChangeMessageTemplateStatusRequest, MessageTemplateResponse>(
            TemplateActionUrl(branchId, templateId, "publish"),
            new ChangeMessageTemplateStatusRequest(expectedRevision),
            cancellationToken);

    public Task<MessageTemplateResponse> RetireMessageTemplateAsync(
        string branchId,
        string templateId,
        long expectedRevision,
        CancellationToken cancellationToken = default) =>
        PostAsync<ChangeMessageTemplateStatusRequest, MessageTemplateResponse>(
            TemplateActionUrl(branchId, templateId, "retire"),
            new ChangeMessageTemplateStatusRequest(expectedRevision),
            cancellationToken);

    public Task<IReadOnlyCollection<MessageDeliveryAttemptResponse>> ListMessageDeliveryAttemptsAsync(
        string branchId,
        int take = 50,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<MessageDeliveryAttemptResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/delivery-attempts?take={take}",
            cancellationToken);

    public async Task<T> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(relativeUrl, cancellationToken);
        return await ApiResponseReader.ReadAsync<T>(response, cancellationToken);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(relativeUrl, request, cancellationToken);
        return await ApiResponseReader.ReadAsync<TResponse>(response, cancellationToken);
    }

    public async Task DeleteAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(relativeUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await ApiResponseReader.ReadAsync<object>(response, cancellationToken);
        }
    }

    private static string TemplateActionUrl(string branchId, string templateId, string action) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/templates/" +
        $"{Uri.EscapeDataString(templateId)}/{action}";

    private static string PreferenceUrl(string branchId, string stakeholderId) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/communications/stakeholders/" +
        $"{Uri.EscapeDataString(stakeholderId)}/preferences";

    private static string PractitionerUrl(string branchId, string? practitionerId = null) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/practitioners" +
        (string.IsNullOrWhiteSpace(practitionerId) ? string.Empty : $"/{Uri.EscapeDataString(practitionerId)}");

    private Task<PractitionerResponse> PractitionerStatusAsync(string branchId, string practitionerId,
        string action, long expectedVersion, CancellationToken cancellationToken) =>
        PostAsync<ChangePractitionerStatusRequest, PractitionerResponse>(
            $"{PractitionerUrl(branchId, practitionerId)}/{action}",
            new ChangePractitionerStatusRequest(expectedVersion), cancellationToken);

    private Task<PractitionerResponse> PractitionerAssignmentStatusAsync(string branchId, string practitionerId,
        string assignmentId, string action, long expectedVersion, CancellationToken cancellationToken) =>
        PostAsync<ChangePractitionerStatusRequest, PractitionerResponse>(
            $"{PractitionerUrl(branchId, practitionerId)}/assignments/{Uri.EscapeDataString(assignmentId)}/{action}",
            new ChangePractitionerStatusRequest(expectedVersion), cancellationToken);

    private static string BillingUrl(string branchId, string path) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/billing/{path}";

    private static string PhysiotherapyUrl(string branchId, string? carePlanId = null) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/physiotherapy/care-plans" +
        (string.IsNullOrWhiteSpace(carePlanId) ? string.Empty : $"/{Uri.EscapeDataString(carePlanId)}");

    private static string RadiologyOrderStudyUrl(string branchId, string orderId) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/investigation-orders/" +
        $"{Uri.EscapeDataString(orderId)}/radiology-study";

    private static string RadiologyStudyUrl(string branchId, string studyId) =>
        $"api/v1/branches/{Uri.EscapeDataString(branchId)}/radiology/studies/" +
        Uri.EscapeDataString(studyId);

    public Task<IReadOnlyCollection<PatientRelationResponse>> ListPatientRelationsAsync(
        string branchId, string patientId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<PatientRelationResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients/{Uri.EscapeDataString(patientId)}/relations",
            cancellationToken);

    public Task<PatientRelationResponse> AddPatientRelationAsync(
        string branchId, string patientId, AddPatientRelationRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<AddPatientRelationRequest, PatientRelationResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients/{Uri.EscapeDataString(patientId)}/relations",
            request, cancellationToken);

    public Task RemovePatientRelationAsync(
        string branchId, string relationId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients/relations/{Uri.EscapeDataString(relationId)}",
            cancellationToken);

    public Task<IReadOnlyCollection<PatientPackageSummaryResponse>> ListPatientActivePackagesAsync(
        string branchId, string patientId, string? serviceId = null, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts/patients/{Uri.EscapeDataString(patientId)}/active-packages";
        if (!string.IsNullOrWhiteSpace(serviceId))
        {
            url += $"?serviceId={Uri.EscapeDataString(serviceId)}";
        }
        return GetAsync<IReadOnlyCollection<PatientPackageSummaryResponse>>(url, cancellationToken);
    }

    public Task<BookingPackageStatusResponse> GetBookingPackageStatusAsync(
        string branchId, string bookingId, CancellationToken cancellationToken = default) =>
        GetAsync<BookingPackageStatusResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts/bookings/{Uri.EscapeDataString(bookingId)}/package-status",
            cancellationToken);

    public Task<EntitlementReservationResponse> LinkBookingPackageAsync(
        string branchId, string bookingId, LinkBookingPackageRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<LinkBookingPackageRequest, EntitlementReservationResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts/bookings/{Uri.EscapeDataString(bookingId)}/link-package",
            request, cancellationToken);

    public Task<EntitlementReservationResponse> UnlinkBookingPackageAsync(
        string branchId, string bookingId, UnlinkBookingPackageRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<UnlinkBookingPackageRequest, EntitlementReservationResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/contracts/bookings/{Uri.EscapeDataString(bookingId)}/unlink-package",
            request, cancellationToken);

    public Task<IReadOnlyCollection<DoctorSlotResponse>> GenerateDoctorSlotsAsync(
        string branchId, GenerateDoctorSlotsRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<GenerateDoctorSlotsRequest, IReadOnlyCollection<DoctorSlotResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/doctor-slots/generate",
            request, cancellationToken);

    public Task<IReadOnlyCollection<DoctorSlotResponse>> GetDoctorSlotsAsync(
        string branchId, string? practitionerId = null, string? serviceId = null, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(practitionerId)) query.Add($"practitionerId={Uri.EscapeDataString(practitionerId)}");
        if (!string.IsNullOrWhiteSpace(serviceId)) query.Add($"serviceId={Uri.EscapeDataString(serviceId)}");
        if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return GetAsync<IReadOnlyCollection<DoctorSlotResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/doctor-slots{qs}", cancellationToken);
    }

    public Task<IReadOnlyCollection<DoctorSlotResponse>> GetAvailableDoctorSlotsAsync(
        string branchId, string? practitionerId = null, string? serviceId = null, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(practitionerId)) query.Add($"practitionerId={Uri.EscapeDataString(practitionerId)}");
        if (!string.IsNullOrWhiteSpace(serviceId)) query.Add($"serviceId={Uri.EscapeDataString(serviceId)}");
        if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return GetAsync<IReadOnlyCollection<DoctorSlotResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/doctor-slots/available{qs}", cancellationToken);
    }

    public Task<DoctorSlotResponse> BlockDoctorSlotAsync(
        string branchId, string slotId, BlockDoctorSlotRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<BlockDoctorSlotRequest, DoctorSlotResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/doctor-slots/{Uri.EscapeDataString(slotId)}/block",
            request, cancellationToken);

    public Task<DoctorSlotResponse> UnblockDoctorSlotAsync(
        string branchId, string slotId, CancellationToken cancellationToken = default) =>
        PostAsync<object?, DoctorSlotResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/doctor-slots/{Uri.EscapeDataString(slotId)}/unblock",
            null, cancellationToken);

    public Task<DoctorSlotResponse> CancelDoctorSlotAsync(
        string branchId, string slotId, CancellationToken cancellationToken = default) =>
        PostAsync<object?, DoctorSlotResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/doctor-slots/{Uri.EscapeDataString(slotId)}/cancel",
            null, cancellationToken);

    public Task<DirectSlotBookingConfirmationResponse> BookSlotDirectAsync(
        string branchId, BookSlotDirectRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<BookSlotDirectRequest, DirectSlotBookingConfirmationResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/doctor-slots/book-direct",
            request, cancellationToken);

    public Task<BookingRequestResponse> SubmitBookingRequestAsync(
        string branchId, SubmitBookingRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SubmitBookingRequest, BookingRequestResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/booking-requests",
            request, cancellationToken);

    public Task<IReadOnlyCollection<BookingRequestResponse>> GetBookingRequestsAsync(
        string branchId, string? status = null, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return GetAsync<IReadOnlyCollection<BookingRequestResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/booking-requests{qs}", cancellationToken);
    }

    public Task<BookingRequestResponse> GetBookingRequestByIdAsync(
        string branchId, string requestId, CancellationToken cancellationToken = default) =>
        GetAsync<BookingRequestResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/booking-requests/{Uri.EscapeDataString(requestId)}",
            cancellationToken);

    public Task<BookingRequestResponse> ApproveBookingRequestAsync(
        string branchId, string requestId, ApproveBookingRequest command, CancellationToken cancellationToken = default) =>
        PostAsync<ApproveBookingRequest, BookingRequestResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/booking-requests/{Uri.EscapeDataString(requestId)}/approve",
            command, cancellationToken);

    public Task<BookingRequestResponse> DeclineBookingRequestAsync(
        string branchId, string requestId, DeclineBookingRequest command, CancellationToken cancellationToken = default) =>
        PostAsync<DeclineBookingRequest, BookingRequestResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/booking-requests/{Uri.EscapeDataString(requestId)}/decline",
            command, cancellationToken);

    public Task<BookingRequestResponse> RescheduleBookingRequestAsync(
        string branchId, string requestId, RescheduleBookingRequestNotice command, CancellationToken cancellationToken = default) =>
        PostAsync<RescheduleBookingRequestNotice, BookingRequestResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/booking-requests/{Uri.EscapeDataString(requestId)}/reschedule",
            command, cancellationToken);

    public Task<VitalSignsResponse> RecordVitalsAsync(
        string branchId, string patientId, RecordVitalSignsRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RecordVitalSignsRequest, VitalSignsResponse>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients/{Uri.EscapeDataString(patientId)}/vitals",
            request, cancellationToken);

    public Task<VitalSignsResponse?> GetLatestVitalsAsync(
        string branchId, string patientId, CancellationToken cancellationToken = default) =>
        GetAsync<VitalSignsResponse?>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients/{Uri.EscapeDataString(patientId)}/vitals/latest",
            cancellationToken);

    public Task<IReadOnlyCollection<VitalSignsResponse>> ListVitalsHistoryAsync(
        string branchId, string patientId, int take = 10, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<VitalSignsResponse>>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/patients/{Uri.EscapeDataString(patientId)}/vitals/history?take={take}",
            cancellationToken);

    public Task<VitalSignsResponse?> GetVitalsByBookingAsync(
        string branchId, string bookingId, CancellationToken cancellationToken = default) =>
        GetAsync<VitalSignsResponse?>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/vitals/by-booking/{Uri.EscapeDataString(bookingId)}",
            cancellationToken);

    public Task<VitalSignsResponse?> GetVitalsByEncounterAsync(
        string branchId, string encounterId, CancellationToken cancellationToken = default) =>
        GetAsync<VitalSignsResponse?>(
            $"api/v1/branches/{Uri.EscapeDataString(branchId)}/vitals/by-encounter/{Uri.EscapeDataString(encounterId)}",
            cancellationToken);
}