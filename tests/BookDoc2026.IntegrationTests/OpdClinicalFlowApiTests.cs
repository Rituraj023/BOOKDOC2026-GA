using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Queues;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using Xunit;

namespace BookDoc2026.IntegrationTests;

public sealed class OpdClinicalFlowApiTests
{
    [Fact]
    public async Task Opd_CheckIn_RecordTriageVitals_And_CompleteConsultation()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient();
        var tenant = await ProvisionAsync(client);

        // 1. Setup staff authorization with all necessary OPD & Triage permissions
        SetHeaders(client, tenant,
            FoundationPermissions.PatientsRegister,
            FoundationPermissions.PatientsSearch,
            FoundationPermissions.PatientsView,
            FoundationPermissions.QueuesServicePointsManage,
            FoundationPermissions.QueuesCheckIn,
            FoundationPermissions.QueuesView,
            FoundationPermissions.QueuesCall,
            FoundationPermissions.QueuesProgress,
            FoundationPermissions.EncountersView,
            FoundationPermissions.EncounterDraftsManage);

        // 2. Register walk-in patient at reception
        var person = new StakeholderPersonRequest(
            Honorific: "Mr",
            GivenName: "Rahul",
            MiddleName: null,
            FamilyName: "Dravid",
            DateOfBirth: new DateOnly(1985, 5, 20),
            IsDateOfBirthEstimated: false,
            AdministrativeSex: "Male");

        var contact = new StakeholderContactRequest("Mobile", "+919876500001", true);

        var registerPatientRequest = new RegisterPatientRequest(
            RegistrationRequestId: Guid.NewGuid(),
            Person: person,
            BloodGroup: "O+",
            Contacts: [contact],
            Identifiers: [],
            Addresses: [],
            Documents: [],
            DuplicateOverrideReason: null);

        var patientResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/patients", registerPatientRequest);
        Assert.Equal(HttpStatusCode.Created, patientResponse.StatusCode);
        var patient = (await patientResponse.Content.ReadFromJsonAsync<ApiEnvelope<PatientResponse>>())!.Data;
        Assert.Equal("Rahul Dravid", $"{patient.Person.GivenName} {patient.Person.FamilyName}");

        // 3. Create OPD Consultation Room / Service Point
        var createSpRequest = new CreateImagingServicePointRequest(
            Code: "OPD-ROOM-101",
            Name: "Doctor Consultation Cabin 1",
            Modality: "XRay",
            ResourceId: null);

        var spResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points", createSpRequest);
        Assert.Equal(HttpStatusCode.Created, spResponse.StatusCode);
        var servicePoint = (await spResponse.Content.ReadFromJsonAsync<ApiEnvelope<ImagingServicePointResponse>>())!.Data;

        // 4. Check-in patient to generate OPD Daily Token #
        var checkInRequest = new CheckInQueueTicketRequest(
            RequestId: Guid.NewGuid(),
            ServicePointId: servicePoint.Id,
            PatientId: patient.Id,
            BookingId: null,
            Priority: "Normal",
            PriorityReason: null);

        var checkInResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets", checkInRequest);
        Assert.Equal(HttpStatusCode.Created, checkInResponse.StatusCode);
        var ticket = (await checkInResponse.Content.ReadFromJsonAsync<ApiEnvelope<QueueTicketResponse>>())!.Data;
        Assert.Equal("Waiting", ticket.Status);
        Assert.False(string.IsNullOrWhiteSpace(ticket.DisplayToken));

        // 5. Nurse records Triage Vitals
        var vitalsRequest = new RecordVitalSignsRequest(
            BookingId: null,
            ClinicalEncounterId: null,
            SystolicBp: 120,
            DiastolicBp: 80,
            PulseBpm: 72,
            TemperatureF: 98.6m,
            SpO2Percent: 99,
            RespiratoryRate: 16,
            WeightKg: 70m,
            HeightCm: 175m,
            BloodGlucoseMgDl: 95m,
            ClinicalNotes: "Mild seasonal headache");

        var vitalsResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/patients/{patient.Id}/vitals", vitalsRequest);
        Assert.Equal(HttpStatusCode.Created, vitalsResponse.StatusCode);
        var recordedVitals = (await vitalsResponse.Content.ReadFromJsonAsync<ApiEnvelope<VitalSignsResponse>>())!.Data;
        Assert.Equal(22.86m, recordedVitals.Bmi);
        Assert.Equal(120, recordedVitals.SystolicBp);
        Assert.Equal(80, recordedVitals.DiastolicBp);

        // 6. Doctor verifies latest vitals
        var latestVitalsResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/patients/{patient.Id}/vitals/latest");
        Assert.Equal(HttpStatusCode.OK, latestVitalsResponse.StatusCode);
        var latestVitals = (await latestVitalsResponse.Content.ReadFromJsonAsync<ApiEnvelope<VitalSignsResponse>>())!.Data;
        Assert.NotNull(latestVitals);
        Assert.Equal(22.86m, latestVitals.Bmi);

        // 7. Doctor calls patient into consultation room
        var callResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{ticket.Id}/call",
            new QueueTransitionRequest(ticket.Version, "Doctor calling patient"));
        Assert.Equal(HttpStatusCode.OK, callResponse.StatusCode);
        var calledTicket = (await callResponse.Content.ReadFromJsonAsync<ApiEnvelope<QueueTicketResponse>>())!.Data;
        Assert.Equal("Called", calledTicket.Status);

        // 8. Nurse triage prepares patient ticket
        var prepareResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{ticket.Id}/prepare",
            new QueueTransitionRequest(calledTicket.Version, "Triage prepared"));
        Assert.Equal(HttpStatusCode.OK, prepareResponse.StatusCode);
        var preparedTicket = (await prepareResponse.Content.ReadFromJsonAsync<ApiEnvelope<QueueTicketResponse>>())!.Data;
        Assert.Equal("Preparation", preparedTicket.Status);

        // 9. Doctor starts consultation
        var startResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{ticket.Id}/start",
            new QueueTransitionRequest(preparedTicket.Version, "Consultation started"));
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var startedTicket = (await startResponse.Content.ReadFromJsonAsync<ApiEnvelope<QueueTicketResponse>>())!.Data;
        Assert.Equal("InService", startedTicket.Status);

        // 10. Doctor completes consultation
        var completeResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{ticket.Id}/complete",
            new QueueTransitionRequest(startedTicket.Version, "Consultation completed and prescription given"));
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completedTicket = (await completeResponse.Content.ReadFromJsonAsync<ApiEnvelope<QueueTicketResponse>>())!.Data;
        Assert.Equal("Completed", completedTicket.Status);
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client)
    {
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest("OPD Clinic", $"opd-{Guid.NewGuid():N}", "admin@opd.invalid", "Mumbai", "MUM01"));
        var submitted = (await submittedResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "5001");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var response = await client.PostAsJsonAsync($"/api/v1/platform/tenant-applications/{submitted.Data.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Data.Version));
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static void SetHeaders(HttpClient client, TenantProvisioningResponse tenant, params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "5002");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
        client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[] { "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator" })
            client.DefaultRequestHeaders.Remove(header);
    }
}
