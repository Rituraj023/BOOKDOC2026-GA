using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Catalog;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Clinical;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Queues;
using BookDoc2026.Contracts.Radiology;
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

        SetHeaders(client, tenant, FoundationPermissions.CatalogManage);
        var imagingService = await PostCreated<ServiceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services",
            new CreateServiceRequest("XR-KNEE", "Knee X-ray", "Standing knee X-ray", 15));
        var imagingCategory = await PostCreated<ResourceCategoryResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/resource-categories",
            new CreateResourceCategoryRequest(null, "XRAY", "X-ray modality", "ImagingModality"));
        var ctImagingCategory = await PostCreated<ResourceCategoryResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/resource-categories",
            new CreateResourceCategoryRequest(null, "CT", "CT modality", "ImagingModality"));
        _ = await PostCreated<ServiceResourceRequirementResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/catalog/services/{imagingService.Id}/resource-requirements",
            new AddServiceResourceRequirementRequest(imagingCategory.Id, "PRIMARY-MODALITY", 1, false));
        SetHeaders(client, tenant, FoundationPermissions.InvestigationOrdersCreate);
        var unsignedOrder = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders",
            new CreateInvestigationOrderRequest(Guid.NewGuid(), imagingService.Id, "XRay",
                "Persistent right-knee pain"));
        Assert.Equal(HttpStatusCode.BadRequest, unsignedOrder.StatusCode);
        var practitionerActorId = await CreateEligiblePractitionerAsync(client, factory, tenant, booking.ServiceId);
        var bookingLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(booking.StartUtc,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")).DateTime);

        SetHeaders(client, tenant, FoundationPermissions.EncountersView);
        var unassignedAgenda = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<ClinicalAgendaItemResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/encounters/agenda?date={bookingLocalDate:yyyy-MM-dd}&take=25");
        Assert.Empty(unassignedAgenda!.Data);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.EncountersView);
        var agendaResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/agenda?date={bookingLocalDate:yyyy-MM-dd}&take=25");
        Assert.Equal(HttpStatusCode.OK, agendaResponse.StatusCode);
        var agendaJson = await agendaResponse.Content.ReadAsStringAsync();
        var agenda = (await agendaResponse.Content.ReadFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<ClinicalAgendaItemResponse>>>())!.Data;
        var agendaItem = Assert.Single(agenda);
        Assert.Equal(booking.BookingNumber, agendaItem.BookingNumber);
        Assert.Equal("Draft", agendaItem.EncounterStatus);
        Assert.DoesNotMatch("^[0-9]+$", agendaItem.BookingId);
        Assert.DoesNotContain("Initial history", agendaJson, StringComparison.OrdinalIgnoreCase);

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

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.InvestigationOrdersCreate);
        var catalogOptions = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<InvestigationServiceOptionResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders/catalog-options");
        var xrayOption = Assert.Single(catalogOptions!.Data);
        Assert.Equal(imagingService.Code, xrayOption.ServiceCode);
        Assert.DoesNotMatch("^[0-9]+$", xrayOption.ServiceId);
        Assert.Equal("XRay", xrayOption.Modality);

        var modalityMismatchOrder = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders",
            new CreateInvestigationOrderRequest(Guid.NewGuid(), imagingService.Id, "CT",
                "Persistent right-knee pain after assessment"));
        Assert.Equal(HttpStatusCode.BadRequest, modalityMismatchOrder.StatusCode);
        var nonImagingOrder = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders",
            new CreateInvestigationOrderRequest(Guid.NewGuid(), booking.ServiceId, "XRay",
                "Persistent right-knee pain after assessment"));
        Assert.Equal(HttpStatusCode.BadRequest, nonImagingOrder.StatusCode);

        var orderRequestId = Guid.NewGuid();
        var orderRequest = new CreateInvestigationOrderRequest(orderRequestId, imagingService.Id, "XRay",
            "Persistent right-knee pain after assessment");
        var orderResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders", orderRequest);
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = (await orderResponse.Content.ReadFromJsonAsync<ApiEnvelope<InvestigationOrderResponse>>())!.Data;
        Assert.Equal("Requested", order.OrderStatus);
        Assert.Equal("Pending", order.ResultStatus);
        Assert.Null(order.QueueTicket);
        Assert.Single(order.History);
        Assert.DoesNotMatch("^[0-9]+$", order.Id);

        var orderReplayResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders", orderRequest);
        Assert.Equal(HttpStatusCode.OK, orderReplayResponse.StatusCode);
        var orderReplay = (await orderReplayResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<InvestigationOrderResponse>>())!.Data;
        Assert.True(orderReplay.IsReplay);
        Assert.Equal(order.OrderNumber, orderReplay.OrderNumber);

        var listWithoutView = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders");
        Assert.Equal(HttpStatusCode.Forbidden, listWithoutView.StatusCode);
        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.InvestigationsView);
        var orderList = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<InvestigationOrderResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders");
        Assert.Equal(order.OrderNumber, Assert.Single(orderList!.Data).OrderNumber);

        SetHeaders(client, tenant, FoundationPermissions.QueuesServicePointsManage);
        var xrayPoint = await PostCreated<ImagingServicePointResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points",
            new CreateImagingServicePointRequest("XR-CLIN", "Clinical X-ray", "XRay", null));
        var ctPoint = await PostCreated<ImagingServicePointResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points",
            new CreateImagingServicePointRequest("CT-CLIN", "Clinical CT", "CT", null));
        var handoffRequestId = Guid.NewGuid();
        var handoffRequest = new HandoffInvestigationOrderRequest(
            handoffRequestId, order.Version, xrayPoint.Id, "Normal", null);
        SetHeaders(client, tenant, FoundationPermissions.InvestigationQueueHandoff);
        var handoffWithoutQueuePermission = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders/{order.Id}/queue-handoffs",
            handoffRequest);
        Assert.Equal(HttpStatusCode.Forbidden, handoffWithoutQueuePermission.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.InvestigationQueueHandoff,
            FoundationPermissions.QueuesCheckIn);
        var modalityMismatch = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders/{order.Id}/queue-handoffs",
            handoffRequest with { ServicePointId = ctPoint.Id });
        Assert.Equal(HttpStatusCode.BadRequest, modalityMismatch.StatusCode);
        var handoffResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders/{order.Id}/queue-handoffs",
            handoffRequest);
        Assert.Equal(HttpStatusCode.Created, handoffResponse.StatusCode);
        var handedOff = (await handoffResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<InvestigationOrderResponse>>())!.Data;
        Assert.Equal("Requested", handedOff.OrderStatus);
        Assert.Equal("Pending", handedOff.ResultStatus);
        Assert.Equal("Waiting", handedOff.QueueTicket!.Status);
        var decodedTenant = TestPublicIds.DecodeTenant(factory.Services, tenant);
        Assert.Equal(
            TestPublicIds.Decode(factory.Services, PublicIdKind.InvestigationOrder, order.Id,
                decodedTenant.TenantId),
            TestPublicIds.Decode(factory.Services, PublicIdKind.InvestigationOrder,
                handedOff.QueueTicket.InvestigationOrderId!, decodedTenant.TenantId));
        Assert.Equal(2, handedOff.History.Count);

        var handoffReplayResponse = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders/{order.Id}/queue-handoffs",
            handoffRequest);
        Assert.Equal(HttpStatusCode.OK, handoffReplayResponse.StatusCode);
        var handoffReplay = (await handoffReplayResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<InvestigationOrderResponse>>())!.Data;
        Assert.True(handoffReplay.IsReplay);
        Assert.Equal(handedOff.QueueTicket.DisplayToken, handoffReplay.QueueTicket!.DisplayToken);

        var worklistUrl = $"/api/v1/branches/{tenant.BranchId}/investigations/worklist" +
            $"?servicePointId={Uri.EscapeDataString(xrayPoint.Id)}&take=100";
        SetHeaders(client, tenant, FoundationPermissions.QueuesView);
        var queuePermissionOnlyWorklist = await client.GetAsync(worklistUrl);
        Assert.Equal(HttpStatusCode.Forbidden, queuePermissionOnlyWorklist.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.InvestigationWorklistView);
        var servicePointsForWorklist = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<ImagingServicePointResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points");
        Assert.Contains(servicePointsForWorklist!.Data, point => point.Code == xrayPoint.Code);
        var genericQueueWithoutQueuePermission = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/queues/imaging-service-points/{xrayPoint.Id}/tickets");
        Assert.Equal(HttpStatusCode.Forbidden, genericQueueWithoutQueuePermission.StatusCode);

        var investigationWorklistResponse = await client.GetAsync(worklistUrl);
        Assert.Equal(HttpStatusCode.OK, investigationWorklistResponse.StatusCode);
        var investigationWorklistJson = await investigationWorklistResponse.Content.ReadAsStringAsync();
        var investigationWorklist = (await investigationWorklistResponse.Content.ReadFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<InvestigationWorklistItemResponse>>>())!.Data;
        var work = Assert.Single(investigationWorklist);
        Assert.Equal(order.OrderNumber, work.OrderNumber);
        Assert.Equal(imagingService.Code, work.ServiceCode);
        Assert.Equal("XRay", work.Modality);
        Assert.Equal(order.ClinicalIndication, work.ClinicalIndication);
        Assert.Equal("Clinical Patient", work.PatientDisplayName);
        Assert.Equal(started.EncounterNumber, work.EncounterNumber);
        Assert.Equal(handedOff.QueueTicket.DisplayToken, work.QueueTicket.DisplayToken);
        Assert.DoesNotMatch("^[0-9]+$", work.PatientId);
        Assert.DoesNotMatch("^[0-9]+$", work.EncounterId);
        Assert.DoesNotContain("Reviewed history", investigationWorklistJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Reduced knee flexion", investigationWorklistJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("resultStatus", investigationWorklistJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("history", investigationWorklistJson, StringComparison.OrdinalIgnoreCase);

        SetHeaders(client, tenant, FoundationPermissions.ResourcesManage);
        var xrayEquipment = await PostCreated<BookableResourceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources",
            new CreateBookableResourceRequest(imagingCategory.Id, "XR-MACHINE", "X-ray Machine",
                "Exclusive", 1, "Asia/Kolkata", null));
        _ = await PostCreated<ResourceCapabilityResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources/{xrayEquipment.Id}/capabilities",
            new AddResourceCapabilityRequest(imagingService.Id, null, 1));
        var wrongModalityEquipment = await PostCreated<BookableResourceResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources",
            new CreateBookableResourceRequest(ctImagingCategory.Id, "CT-MACHINE", "CT Machine",
                "Exclusive", 1, "Asia/Kolkata", null));
        _ = await PostCreated<ResourceCapabilityResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/resources/{wrongModalityEquipment.Id}/capabilities",
            new AddResourceCapabilityRequest(imagingService.Id, null, 1));
        var radiologyOperatorId = await CreateEligiblePractitionerAsync(client, factory, tenant,
            imagingService.Id, "RADIOLOGY_TECHNICIAN", "XRAY_TECH", "PERFORMING");
        var qualityReviewerId = await CreateEligiblePractitionerAsync(client, factory, tenant,
            imagingService.Id, "RADIOLOGY_REVIEWER", "XRAY_QA", "QUALITY_REVIEW");
        var studyUrl = $"/api/v1/branches/{tenant.BranchId}/investigation-orders/{order.Id}/radiology-study";

        SetHeaders(client, tenant, FoundationPermissions.InvestigationWorklistView);
        var worklistCannotRegister = await client.PostAsJsonAsync(studyUrl,
            new RegisterRadiologyStudyRequest(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, worklistCannotRegister.StatusCode);

        SetHeaders(client, tenant, FoundationPermissions.RadiologyStudiesStart);
        var permissionOnlyRegistration = await client.PostAsJsonAsync(studyUrl,
            new RegisterRadiologyStudyRequest(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, permissionOnlyRegistration.StatusCode);

        var registerRequest = new RegisterRadiologyStudyRequest(Guid.NewGuid());
        SetHeaders(client, tenant, radiologyOperatorId, FoundationPermissions.RadiologyStudiesStart);
        var study = await PostCreated<RadiologyStudyResponse>(client, studyUrl, registerRequest);
        Assert.Equal("Registered", study.Status);
        Assert.Equal(
            TestPublicIds.Decode(factory.Services, PublicIdKind.InvestigationOrder, order.Id,
                decodedTenant.TenantId),
            TestPublicIds.Decode(factory.Services, PublicIdKind.InvestigationOrder, study.OrderId,
                decodedTenant.TenantId));
        Assert.DoesNotMatch("^[0-9]+$", study.Id);
        Assert.Single(study.History);
        var registerReplayResponse = await client.PostAsJsonAsync(studyUrl, registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerReplayResponse.StatusCode);
        var registerReplay = (await registerReplayResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<RadiologyStudyResponse>>())!.Data;
        Assert.True(registerReplay.IsReplay);
        Assert.Equal(
            TestPublicIds.Decode(factory.Services, PublicIdKind.RadiologyStudy, study.Id,
                decodedTenant.TenantId),
            TestPublicIds.Decode(factory.Services, PublicIdKind.RadiologyStudy, registerReplay.Id,
                decodedTenant.TenantId));

        var getStudyUrl = $"/api/v1/branches/{tenant.BranchId}/radiology/studies/{study.Id}";
        SetHeaders(client, tenant, FoundationPermissions.InvestigationWorklistView);
        var worklistCannotViewStudy = await client.GetAsync(getStudyUrl);
        Assert.Equal(HttpStatusCode.Forbidden, worklistCannotViewStudy.StatusCode);

        SetHeaders(client, tenant, radiologyOperatorId, FoundationPermissions.RadiologyStudiesStart);
        var startRequest = new StartRadiologyStudyRequest(Guid.NewGuid(), study.Version);
        study = await PostOk<RadiologyStudyResponse>(client, $"{getStudyUrl}/start", startRequest);
        Assert.Equal("InProgress", study.Status);
        var startReplay = await PostOk<RadiologyStudyResponse>(client, $"{getStudyUrl}/start", startRequest);
        Assert.True(startReplay.IsReplay);
        Assert.Equal(study.Version, startReplay.Version);

        var equipmentUrl = $"{getStudyUrl}/eligible-equipment";
        SetHeaders(client, tenant, qualityReviewerId, FoundationPermissions.RadiologyStudiesQualityReview);
        var reviewerCannotListEquipment = await client.GetAsync(equipmentUrl);
        Assert.Equal(HttpStatusCode.Forbidden, reviewerCannotListEquipment.StatusCode);
        SetHeaders(client, tenant, radiologyOperatorId, FoundationPermissions.RadiologyAcquisitionsRecord);
        var eligibleEquipment = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<RadiologyEquipmentOptionResponse>>>(equipmentUrl);
        var eligibleXray = Assert.Single(eligibleEquipment!.Data);
        Assert.Equal("XR-MACHINE", eligibleXray.Code);
        Assert.DoesNotMatch("^[0-9]+$", eligibleXray.Id);

        var wrongModalityAcquisition = await client.PostAsJsonAsync($"{getStudyUrl}/acquisitions",
            new RecordRadiologyAcquisitionRequest(Guid.NewGuid(), study.Version,
                wrongModalityEquipment.Id, "XR-KNEE-AP", "V1", false, null, null,
                "Acquired", null, null, null, DateTimeOffset.UtcNow.AddMinutes(-2),
                DateTimeOffset.UtcNow.AddMinutes(-1)));
        Assert.Equal(HttpStatusCode.BadRequest, wrongModalityAcquisition.StatusCode);

        var acquisitionRequest = new RecordRadiologyAcquisitionRequest(Guid.NewGuid(), study.Version,
            xrayEquipment.Id, "XR-KNEE-AP", "V1", false, null, null, "Acquired", null, null,
            "1.2.840.10008.20260828.100", DateTimeOffset.UtcNow.AddMinutes(-2),
            DateTimeOffset.UtcNow.AddMinutes(-1));
        var acquisitionResponse = await client.PostAsJsonAsync($"{getStudyUrl}/acquisitions", acquisitionRequest);
        Assert.Equal(HttpStatusCode.OK, acquisitionResponse.StatusCode);
        var acquisitionJson = await acquisitionResponse.Content.ReadAsStringAsync();
        study = (await acquisitionResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<RadiologyStudyResponse>>())!.Data;
        var acquisition = Assert.Single(study.AcquisitionAttempts);
        Assert.Equal("Acquired", study.Status);
        Assert.Equal("XR-KNEE-AP", acquisition.ProtocolCode);
        Assert.True(acquisition.HasExternalStudyReference);
        Assert.DoesNotContain("1.2.840.10008.20260828.100", acquisitionJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"externalStudyReference\":", acquisitionJson,
            StringComparison.OrdinalIgnoreCase);
        var acquisitionReplay = await PostOk<RadiologyStudyResponse>(client,
            $"{getStudyUrl}/acquisitions", acquisitionRequest);
        Assert.True(acquisitionReplay.IsReplay);
        Assert.Single(acquisitionReplay.AcquisitionAttempts);

        var qualityRequest = new ReviewRadiologyQualityRequest(Guid.NewGuid(), study.Version,
            acquisition.Id, "Accepted", "ACCEPTED", "Position and exposure accepted");
        SetHeaders(client, tenant, radiologyOperatorId, FoundationPermissions.RadiologyStudiesQualityReview);
        var selfReview = await client.PostAsJsonAsync($"{getStudyUrl}/quality-reviews", qualityRequest);
        Assert.Equal(HttpStatusCode.BadRequest, selfReview.StatusCode);
        SetHeaders(client, tenant, FoundationPermissions.RadiologyStudiesQualityReview);
        var ineligibleReview = await client.PostAsJsonAsync($"{getStudyUrl}/quality-reviews", qualityRequest);
        Assert.Equal(HttpStatusCode.Forbidden, ineligibleReview.StatusCode);
        SetHeaders(client, tenant, qualityReviewerId, FoundationPermissions.RadiologyStudiesQualityReview);
        study = await PostOk<RadiologyStudyResponse>(client, $"{getStudyUrl}/quality-reviews", qualityRequest);
        Assert.Equal("QualityAccepted", study.Status);
        Assert.Single(study.QualityReviews);
        Assert.Equal(study.AcquisitionAttempts.Single().Id,
            study.QualityReviews.Single().AcquisitionAttemptId);
        Assert.Equal(4, study.History.Count);
        var qualityReplay = await PostOk<RadiologyStudyResponse>(client,
            $"{getStudyUrl}/quality-reviews", qualityRequest);
        Assert.True(qualityReplay.IsReplay);
        Assert.Single(qualityReplay.QualityReviews);

        SetHeaders(client, tenant, FoundationPermissions.QueuesCall);
        var called = await PostOk<QueueTicketResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{handedOff.QueueTicket.Id}/call",
            new QueueTransitionRequest(handedOff.QueueTicket.Version));
        SetHeaders(client, tenant, FoundationPermissions.QueuesProgress);
        var prepared = await PostOk<QueueTicketResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{called.Id}/prepare",
            new QueueTransitionRequest(called.Version));
        var inService = await PostOk<QueueTicketResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{prepared.Id}/start",
            new QueueTransitionRequest(prepared.Version));
        var queueCompleted = await PostOk<QueueTicketResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/queues/tickets/{inService.Id}/complete",
            new QueueTransitionRequest(inService.Version));
        Assert.Equal("Completed", queueCompleted.Status);

        SetHeaders(client, tenant, FoundationPermissions.InvestigationWorklistView);
        var completedWorklist = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<InvestigationWorklistItemResponse>>>(worklistUrl);
        Assert.Empty(completedWorklist!.Data);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.InvestigationsView);
        orderList = await client.GetFromJsonAsync<ApiEnvelope<IReadOnlyCollection<InvestigationOrderResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/encounters/{started.Id}/investigation-orders");
        var afterQueue = Assert.Single(orderList!.Data);
        Assert.Equal("Completed", afterQueue.QueueTicket!.Status);
        Assert.Equal("Requested", afterQueue.OrderStatus);
        Assert.Equal("Pending", afterQueue.ResultStatus);

        SetHeaders(client, tenant, FoundationPermissions.RadiologyStudiesView);
        var studyAfterQueue = await client.GetFromJsonAsync<ApiEnvelope<RadiologyStudyResponse>>(getStudyUrl);
        Assert.Equal("QualityAccepted", studyAfterQueue!.Data.Status);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.EncountersSign);
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

        var carePlanContent = new PhysiotherapyCarePlanContentRequest(
            "Restore comfortable walking and stair function", "Twice weekly for four weeks",
            "Graded exercise, education and movement progression", "Escalate worsening symptoms",
            DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14)));
        SetHeaders(client, tenant, FoundationPermissions.PhysiotherapyCarePlansManage);
        var ineligibleCarePlan = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans",
            new CreatePhysiotherapyCarePlanRequest(started.Id, carePlanContent));
        Assert.Equal(HttpStatusCode.Forbidden, ineligibleCarePlan.StatusCode);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.PhysiotherapyCarePlansManage);
        var carePlan = await PostCreated<PhysiotherapyCarePlanResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans",
            new CreatePhysiotherapyCarePlanRequest(started.Id, carePlanContent));
        Assert.Equal("Draft", carePlan.Status);
        Assert.Single(carePlan.Revisions);
        Assert.Equal(64, carePlan.Revisions.Single().ContentHash.Length);
        Assert.DoesNotMatch("^[0-9]+$", carePlan.Id);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.EncountersView);
        var agendaWithPlan = await client.GetFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<ClinicalAgendaItemResponse>>>(
            $"/api/v1/branches/{tenant.BranchId}/encounters/agenda?date={bookingLocalDate:yyyy-MM-dd}&take=25");
        var agendaPlan = Assert.Single(agendaWithPlan!.Data);
        Assert.Equal(carePlan.CarePlanNumber, agendaPlan.CarePlanNumber);
        Assert.Equal("Draft", agendaPlan.CarePlanStatus);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.PhysiotherapyCarePlansManage);
        var duplicateCarePlan = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans",
            new CreatePhysiotherapyCarePlanRequest(started.Id, carePlanContent));
        Assert.Equal(HttpStatusCode.BadRequest, duplicateCarePlan.StatusCode);
        var forbiddenCarePlanRead = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans/{carePlan.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCarePlanRead.StatusCode);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.PhysiotherapyCarePlansView);
        var worklistResponse = await client.GetAsync(
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans?take=25");
        Assert.Equal(HttpStatusCode.OK, worklistResponse.StatusCode);
        var worklistJson = await worklistResponse.Content.ReadAsStringAsync();
        var worklist = (await worklistResponse.Content.ReadFromJsonAsync<
            ApiEnvelope<IReadOnlyCollection<PhysiotherapyCarePlanListItemResponse>>>())!.Data;
        var workItem = Assert.Single(worklist);
        Assert.Equal(carePlan.CarePlanNumber, workItem.CarePlanNumber);
        Assert.DoesNotMatch("^[0-9]+$", workItem.Id);
        Assert.Equal("Draft", workItem.Status);
        Assert.Equal(0, workItem.SessionCount);
        Assert.DoesNotContain("Restore comfortable walking", worklistJson, StringComparison.OrdinalIgnoreCase);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.PhysiotherapyCarePlansManage);
        carePlan = await PostOk<PhysiotherapyCarePlanResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans/{carePlan.Id}/activate",
            new ActivatePhysiotherapyCarePlanRequest(carePlan.Version));
        Assert.Equal("Active", carePlan.Status);
        var staleActivation = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans/{carePlan.Id}/activate",
            new ActivatePhysiotherapyCarePlanRequest(1));
        Assert.Equal(HttpStatusCode.Conflict, staleActivation.StatusCode);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.PhysiotherapySessionsRecord);
        carePlan = await PostCreated<PhysiotherapyCarePlanResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans/{carePlan.Id}/sessions",
            new RecordPhysiotherapySessionRequest(started.Id, "Walking tolerance improving",
                "Graded knee exercise and education", "Tolerated without adverse response",
                "Progress loading next session", false, null));
        var session = Assert.Single(carePlan.Sessions);
        Assert.Equal(1, session.SequenceNumber);
        Assert.Equal(64, session.ContentHash.Length);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.PhysiotherapyOutcomesRecord);
        carePlan = await PostCreated<PhysiotherapyCarePlanResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans/{carePlan.Id}/outcomes",
            new RecordPhysiotherapyOutcomeRequest(session.Id, "Reassessment", "NPRS", "1.0", 4,
                "score", "Knee", "Right", DateTimeOffset.UtcNow));
        var outcome = Assert.Single(carePlan.Outcomes);
        Assert.Equal("NPRS", outcome.MeasureCode);
        Assert.Equal("1.0", outcome.ToolVersion);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.PhysiotherapyCarePlansManage);
        carePlan = await PostOk<PhysiotherapyCarePlanResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans/{carePlan.Id}/complete",
            new ChangePhysiotherapyCarePlanStatusRequest(carePlan.Version,
                "Goals achieved in the planned course"));
        Assert.Equal("Completed", carePlan.Status);

        SetHeaders(client, tenant, practitionerActorId, FoundationPermissions.PhysiotherapyOutcomesRecord);
        var outcomeAfterClose = await client.PostAsJsonAsync(
            $"/api/v1/branches/{tenant.BranchId}/physiotherapy/care-plans/{carePlan.Id}/outcomes",
            new RecordPhysiotherapyOutcomeRequest(null, "Discharge", "NPRS", "1.0", 0,
                "score", "Knee", "Right", DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.BadRequest, outcomeAfterClose.StatusCode);

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
            db.Entry(revision).State = EntityState.Unchanged;
            var physioRevision = await db.PhysiotherapyCarePlanRevisions.IgnoreQueryFilters().FirstAsync();
            db.Entry(physioRevision).State = EntityState.Deleted;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            db.Entry(physioRevision).State = EntityState.Unchanged;
            var investigationEvent = await db.InvestigationOrderEvents.IgnoreQueryFilters().FirstAsync();
            db.Entry(investigationEvent).State = EntityState.Deleted;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        var otherTenant = await ProvisionAsync(client, "Other Clinical Clinic");
        SetHeaders(client, otherTenant, FoundationPermissions.EncountersView);
        var crossTenant = await client.GetAsync(
            $"/api/v1/branches/{otherTenant.BranchId}/encounters/{started.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, crossTenant.StatusCode);
        SetHeaders(client, otherTenant, FoundationPermissions.PhysiotherapyCarePlansView);
        var crossTenantCarePlan = await client.GetAsync(
            $"/api/v1/branches/{otherTenant.BranchId}/physiotherapy/care-plans/{carePlan.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, crossTenantCarePlan.StatusCode);
        SetHeaders(client, otherTenant, FoundationPermissions.InvestigationsView);
        var crossTenantOrders = await client.GetAsync(
            $"/api/v1/branches/{otherTenant.BranchId}/encounters/{started.Id}/investigation-orders");
        Assert.Equal(HttpStatusCode.BadRequest, crossTenantOrders.StatusCode);
        SetHeaders(client, otherTenant, FoundationPermissions.InvestigationWorklistView);
        var crossTenantWorklist = await client.GetAsync(
            $"/api/v1/branches/{otherTenant.BranchId}/investigations/worklist" +
            $"?servicePointId={Uri.EscapeDataString(xrayPoint.Id)}&take=100");
        Assert.Equal(HttpStatusCode.BadRequest, crossTenantWorklist.StatusCode);
        SetHeaders(client, otherTenant, FoundationPermissions.RadiologyStudiesView);
        var crossTenantStudy = await client.GetAsync(
            $"/api/v1/branches/{otherTenant.BranchId}/radiology/studies/{study.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, crossTenantStudy.StatusCode);
    }

    private static EncounterContentRequest Content(string? assessment, string? plan, string history) => new(
        "PHYSIOTHERAPY", "INITIAL-ASSESSMENT", "v1", "Right knee pain", history,
        "Reduced knee flexion", assessment, plan, "Return if symptoms worsen", "Knee", "Right");

    private static async Task<long> CreateEligiblePractitionerAsync(HttpClient client, BookDocApiFactory factory,
        TenantProvisioningResponse tenant, string serviceId,
        string practitionerTypeCode = "PHYSIOTHERAPIST", string credentialTypeCode = "DPT",
        string assignmentRoleCode = "TREATING")
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
                practitionerTypeCode));
        practitioner = await PostCreated<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners/{practitioner.Id}/credentials",
            new AddPractitionerCredentialRequest(credentialTypeCode, $"DL-{Guid.NewGuid():N}", "Delhi Council",
                new DateOnly(2025, 1, 1), new DateOnly(2030, 12, 31)));

        SetHeaders(client, tenant, FoundationPermissions.PractitionerCredentialsVerify);
        practitioner = await PostOk<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners/{practitioner.Id}/credentials/" +
            $"{practitioner.Credentials.Single().Id}/verify",
            new DecidePractitionerCredentialRequest(practitioner.Credentials.Single().Version));

        SetHeaders(client, tenant, FoundationPermissions.PractitionerAssignmentsManage);
        practitioner = await PostCreated<PractitionerResponse>(client,
            $"/api/v1/branches/{tenant.BranchId}/practitioners/{practitioner.Id}/assignments",
            new AddPractitionerAssignmentRequest(serviceId, null, assignmentRoleCode,
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
