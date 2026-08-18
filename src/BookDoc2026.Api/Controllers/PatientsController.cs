using BookDoc2026.Api.Security;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Patients;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookDoc2026.Api.Controllers;

[ApiController]
[Route("api/v1/branches/{branchId}/patients")]
public sealed class PatientsController(PatientService patientService, HttpPublicIdDecoder ids) : ControllerBase
{
    [Authorize(Policy = FoundationPermissions.PatientsRegister)]
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<PatientResponse>>> Register(
        string branchId,
        RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        var response = await patientService.RegisterAsync(ids.Tenant(PublicIdKind.Branch, branchId), request, cancellationToken);
        return Created(
            $"/api/v1/branches/{branchId}/patients/{response.Id}",
            new ApiEnvelope<PatientResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.PatientsSearch)]
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<PatientSearchResponse>>>> Search(
        string branchId,
        [FromQuery(Name = "q")] string query,
        CancellationToken cancellationToken)
    {
        var response = await patientService.SearchAsync(ids.Tenant(PublicIdKind.Branch, branchId), query, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<PatientSearchResponse>>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.PatientsView)]
    [HttpGet("{patientId}")]
    public async Task<ActionResult<ApiEnvelope<PatientResponse>>> Get(
        string branchId,
        string patientId,
        CancellationToken cancellationToken)
    {
        var response = await patientService.GetAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Patient, patientId), cancellationToken);
        return Ok(new ApiEnvelope<PatientResponse>(response, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = FoundationPermissions.PatientsUpdate)]
    [HttpPut("{patientId}/demographics")]
    public async Task<ActionResult<ApiEnvelope<PatientResponse>>> UpdateDemographics(
        string branchId,
        string patientId,
        UpdatePatientDemographicsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await patientService.UpdateDemographicsAsync(ids.Tenant(PublicIdKind.Branch, branchId),
            ids.Tenant(PublicIdKind.Patient, patientId), request, cancellationToken);
        return Ok(new ApiEnvelope<PatientResponse>(response, HttpContext.TraceIdentifier));
    }
}
