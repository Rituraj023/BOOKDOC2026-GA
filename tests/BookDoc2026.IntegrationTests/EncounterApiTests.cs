using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Contracts.Workforce;
using BookDoc2026.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class EncounterApiTests
{
    [Fact]
    public async Task EncounterHistory_IsAuthorizedAppendOnlySignedAndTenantProtected()
    {
        await using var factory = new BookDocApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var tenant = await ProvisionAsync(client, "Clinical Clinic");
        var booking = await CreateBookingAsync(client, tenant);
        var initialContent = Content(null, null, "Initial history");

        SetHeaders(client, tenant, FoundationPermissions.EncountersView);
        var forbiddenStart = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters", new StartEncounterRequest(booking.Id, initialContent));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenStart.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.EncounterDraftsManage);
        var started = await PostCreated<EncounterResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/encounters", new StartEncounterRequest(booking.Id, initialContent));
        Assert.Equal("Draft", started.Status);
        Assert.DoesNotMatch("^[0-9]+$", started.Id);
        Assert.Single(started.Revisions);

        var duplicate = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters", new StartEncounterRequest(booking.Id, initialContent));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        var completeContent = Content("Mechanical knee pain", "Review after two weeks", "Reviewed history");
        var revised = await PostCreated<EncounterResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/draft-revisions",
            new ReviseEncounterDraftRequest(started.Version, completeContent));
        Assert.Equal(2, revised.Revisions.Count);
        var practitionerActorId = await CreateEligiblePractitionerAsync(client, factory, tenant, booking.ServiceId);

        SetHeaders(client, tenant, FoundationPermissions.EncountersView);
        var forbiddenSign = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/sign",
            new SignEncounterRequest(revised.Version));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenSign.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.EncountersSign);
        var permissionOnlySign = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/sign",
            new SignEncounterRequest(revised.Version));
        Assert.Equal(HttpStatusCode.Forbidden, permissionOnlySign.StatusCode);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.EncountersSign);
        var signedResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/sign",
            new SignEncounterRequest(revised.Version));
        Assert.Equal(HttpStatusCode.OK, signedResponse.StatusCode);
        var signed = (await signedResponse.Content.ReadFromJsonAsync<ApiEnvelope<EncounterResponse>>())!.Data;
        Assert.Equal("Signed", signed.Status);
        Assert.Equal(3, signed.Revisions.Count);
        var signedRevision = signed.Revisions.Single(item => item.Kind == "Signed");
        Assert.Equal(signed.Revisions.Single(item => item.RevisionNumber == 2).ContentHash,
            signedRevision.ContentHash);

        var staleSign = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/sign",
            new SignEncounterRequest(revised.Version));
        Assert.Equal(HttpStatusCode.Conflict, staleSign.StatusCode);

        var corrected = completeContent with { Plan = "Review after three weeks" };
        SetHeaders(client, tenant, FoundationPermissions.EncountersView);
        var forbiddenAmend = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/amendments",
            new AmendEncounterRequest(signed.Version, "Corrected follow-up interval", corrected));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenAmend.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.EncountersAmend);
        var permissionOnlyAmend = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/amendments",
            new AmendEncounterRequest(signed.Version, "Corrected follow-up interval", corrected));
        Assert.Equal(HttpStatusCode.Forbidden, permissionOnlyAmend.StatusCode);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.EncountersAmend);
        var amended = await PostCreated<EncounterResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/amendments",
            new AmendEncounterRequest(signed.Version, "Corrected follow-up interval", corrected));
        Assert.Equal(4, amended.Revisions.Count);
        var amendment = amended.Revisions.Single(item => item.Kind == "Amendment");
        Assert.Equal("Corrected follow-up interval", amendment.AmendmentReason);
        Assert.NotEqual(signedRevision.ContentHash, amendment.ContentHash);
        Assert.Equal(signedRevision.ContentHash,
            amended.Revisions.Single(item => item.Kind == "Signed").ContentHash);

        SetHeaders(client, tenant, FoundationPermissions.EncountersView);
        var loaded = await client.GetFromJsonAsync<ApiEnvelope<EncounterResponse>>(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}");
        Assert.Equal(4, loaded!.Data.Revisions.Count);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
            var revision = await db.EncounterRevisions.IgnoreQueryFilters().FirstAsync();
            db.Entry(revision).State = EntityState.Deleted;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        var otherTenant = await ProvisionAsync(client, "Other Clinical Clinic");
        SetHeaders(client, otherTenant, FoundationPermissions.EncountersView);
        var crossTenant = await client.GetAsync(
            $"/api/v1/branches/{otherTenant.BranchId}/encounters/{started.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, crossTenant.StatusCode);
    }

    private static EncounterContentRequest Content(string? assessment, string? plan, string history) => new(
        "PHYSIOTHERAPY", "INITIAL-ASSESSMENT", "v1", "Right knee pain", history,
        "Reduced knee flexion", assessment, plan, "Return if symptoms worsen", "Knee", "Right");

    private static async Task<long> CreateEligiblePractitionerAsync(HttpClient client, BookDocApiFactory factory,
        TenantProvisioningResponse tenant, string serviceId)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8201");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.UsersManage);
        var identity = await PostCreated<IdentityUserResponse>(client, "/api/v1/platform/identity/users",
            new CreateIdentityUserRequest("Dr Clinical", $"doctor-{Guid.NewGuid():N}@example.invalid", null,
                "BookDoc!2026-Test"));
        var identitySubjectId = TestPublicIds.DecodePlatform(factory.Services, PublicIdKind.IdentitySubject,
            identity.SubjectId);

        SetHeaders(client, tenant, FoundationPermissions.StakeholdersManage);
        var stakeholder = await PostCreated<StakeholderResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/stakeholders/persons",
            new CreatePersonStakeholderRequest(
                new StakeholderPersonRequest("Dr", "Clinical", null, "Practitioner",
                    new DateOnly(1980, 1, 1), false, "Other"), [], [], [], []));

        SetHeaders(client, tenant, FoundationPermissions.PractitionersManage);
        var practitioner = await PostCreated<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners",
            new CreatePractitionerRequest(stakeholder.Id, identity.SubjectId, $"PR-{Guid.NewGuid():N}"[..12],
                "PHYSIOTHERAPIST"));
        practitioner = await PostCreated<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners/{practitioner.Id}/credentials",
            new AddPractitionerCredentialRequest("DPT", $"DL-{Guid.NewGuid():N}", "Delhi Council",
                new DateOnly(2025, 1, 1), new DateOnly(2030, 12, 31)));

        SetHeaders(client, tenant, FoundationPermissions.PractitionerCredentialsVerify);
        practitioner = await PostOk<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners/{practitioner.Id}/credentials/" +
            $"{practitioner.Credentials.Single().Id}/verify",
            new DecidePractitionerCredentialRequest(practitioner.Credentials.Single().Version));

        SetHeaders(client, tenant, FoundationPermissions.PractitionerAssignmentsManage);
        practitioner = await PostCreated<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners/{practitioner.Id}/assignments",
            new AddPractitionerAssignmentRequest(serviceId, null, "TREATING",
                new DateOnly(2025, 1, 1), null));

        SetHeaders(client, tenant, FoundationPermissions.PractitionersManage);
        practitioner = await PostOk<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners/{practitioner.Id}/activate",
            new ChangePractitionerStatusRequest(practitioner.Version));
        Assert.Equal("Active", practitioner.Status);
        return identitySubjectId;
    }

    private static async Task<BookingResponse> CreateBookingAsync(HttpClient client,
        TenantProvisioningResponse tenant)
    {
        SetHeaders(client, tenant, FoundationPermissions.CatalogManage, FoundationPermissions.ResourcesManage);
        var category = await PostCreated<ResourceCategoryResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/resource-categories",
            new CreateResourceCategoryRequest(null, "CLINROOM", "Clinical room", "Space"));
        var service = await PostCreated<ServiceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services",
            new CreateServiceRequest("PHY-ASSESS", "Physiotherapy assessment", null, 30));
        var resource = await PostCreated<BookableResourceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources",
            new CreateBookableResourceRequest(category.Id, "ROOM-01", "Clinical Room 1", "Exclusive", 1,
                "Asia/Kolkata", null));
        _ = await PostCreated<ResourceCapabilityResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources/{resource.Id}/capabilities",
            new AddResourceCapabilityRequest(service.Id, null, 1));
        _ = await PostCreated<ServiceResourceRequirementResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services/{service.Id}/resource-requirements",
            new AddServiceResourceRequirementRequest(category.Id, "ClinicalRoom", 1, false));

        SetHeaders(client, tenant, FoundationPermissions.PatientsRegister);
        var patient = await PostCreated<PatientResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/patients",
            new RegisterPatientRequest(Guid.NewGuid(),
                new(null, "Clinical", null, "Patient", new DateOnly(1990, 1, 1), false, "Other"), null,
                [new("Email", "clinical.patient@example.invalid", true)], [], [], [], null));

        var startUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(5), TimeSpan.Zero);
        var local = TimeZoneInfo.ConvertTime(startUtc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingAvailabilityManage);
        _ = await PostCreated<AvailabilityRuleResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/availability-rules",
            new CreateAvailabilityRuleRequest(resource.Id, service.Id, local.DayOfWeek.ToString(),
                new TimeOnly(0, 0), new TimeOnly(23, 59), DateOnly.FromDateTime(local.DateTime), null, 15, 1));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingHoldsCreate);
        var hold = await PostCreated<SchedulingHoldResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds",
            new CreateSchedulingHoldRequest(Guid.NewGuid(), patient.Id, service.Id, startUtc,
                startUtc.AddMinutes(30), 10, [new(resource.Id, 1, "ClinicalRoom")]));
        SetHeaders(client, tenant, FoundationPermissions.SchedulingBookingsConfirm);
        return await PostCreated<BookingResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/scheduling/holds/{hold.Id}/confirm",
            new ConfirmSchedulingHoldRequest(hold.Version));
    }

    private static async Task<T> PostCreated<T>(HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"Expected Created from {url}, received {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>())!.Data;
    }

    private static async Task<T> PostOk<T>(HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>())!.Data;
    }

    private static async Task<TenantProvisioningResponse> ProvisionAsync(HttpClient client, string name)
    {
        Clear(client);
        var submittedResponse = await client.PostAsJsonAsync("/api/v1/tenant-applications",
            new SubmitTenantApplicationRequest(name, $"clinical-{Guid.NewGuid():N}",
                "owner@example.invalid", "Delhi Main", "DEL01"));
        var submitted = (await submittedResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<TenantApplicationResponse>>())!.Data;
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", "8201");
        client.DefaultRequestHeaders.Add("X-Platform-Operator", "true");
        client.DefaultRequestHeaders.Add("X-Permissions", FoundationPermissions.TenantsApprove);
        var approved = await client.PostAsJsonAsync(
            $"/api/v1/platform/tenant-applications/{submitted.Id}/approve",
            new ApproveTenantApplicationRequest(submitted.Version));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        return (await approved.Content.ReadFromJsonAsync<ApiEnvelope<TenantProvisioningResponse>>())!.Data;
    }

    private static void SetHeaders(HttpClient client, TenantProvisioningResponse tenant, params string[] permissions)
        => SetHeaders(client, tenant, 8202, permissions);

    private static void SetHeaders(HttpClient client, TenantProvisioningResponse tenant, long actorId,
        params string[] permissions)
    {
        Clear(client);
        client.DefaultRequestHeaders.Add("X-User-Id", actorId.ToString());
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.TenantId);
        client.DefaultRequestHeaders.Add("X-Branch-Ids", tenant.BranchId);
        if (permissions.Length > 0)
            client.DefaultRequestHeaders.Add("X-Permissions", string.Join(',', permissions));
    }

    private static void Clear(HttpClient client)
    {
        foreach (var header in new[]
                 { "X-User-Id", "X-Tenant-Id", "X-Branch-Ids", "X-Permissions", "X-Platform-Operator" })
            client.DefaultRequestHeaders.Remove(header);
    }
}
