using System.Security.Cryptography;
using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Stakeholders;
using BookDoc2026.Contracts.Patients;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Stakeholders;

namespace BookDoc2026.Application.Patients;

public sealed class PatientService(
    IPatientRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IClock clock,
    ICorrelationContext correlationContext)
{
    public async Task<PatientResponse> RegisterAsync(
        long branchId,
        RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        RequireBranchPermission(FoundationPermissions.PatientsRegister, branchId);
        if (request.Documents.Count > 0 && !actor.HasPermission(FoundationPermissions.StakeholderDocumentsManage))
        {
            throw new ForbiddenException("Managing stakeholder document references requires the document permission.");
        }
        if (request.RegistrationRequestId == Guid.Empty)
        {
            throw new DomainRuleException("RegistrationRequestId is required for idempotent registration.");
        }

        var payloadHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request)));
        var existing = await repository.GetByRegistrationRequestAsync(request.RegistrationRequestId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Patient.RegistrationBranchId != branchId
                || !string.Equals(existing.Patient.RegistrationPayloadHash, payloadHash, StringComparison.Ordinal))
            {
                throw new DomainRuleException("Registration request identifier was already used with different registration data.");
            }

            return Map(existing);
        }

        var branch = await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
        if (request.Contacts.Count == 0)
        {
            throw new DomainRuleException("At least one mobile or email contact is required for patient registration.");
        }

        var now = clock.UtcNow;
        var stakeholder = StakeholderAggregateFactory.CreatePerson(
            branch.TenantId,
            request.Person,
            request.Contacts,
            request.Identifiers,
            request.Addresses,
            request.Documents,
            publicIds,
            now);
        var person = stakeholder.Person!;
        var patient = Patient.Register(
            branch.TenantId,
            branchId,
            request.RegistrationRequestId,
            payloadHash,
            stakeholder.Stakeholder.Id,
            request.BloodGroup,
            now);

        var duplicateIds = await repository.FindPotentialDuplicatesAsync(
            person.NormalizedSearchName,
            person.DateOfBirth,
            stakeholder.Contacts.Select(contact => contact.NormalizedValue).ToArray(),
            stakeholder.Identifiers.Select(identifier => (identifier.Type, identifier.Issuer, identifier.NormalizedValue)).ToArray(),
            cancellationToken);
        if (duplicateIds.Count > 0 && string.IsNullOrWhiteSpace(request.DuplicateOverrideReason))
        {
            throw new DomainRuleException(
                "A possible duplicate person/patient exists. Review masked search results and provide an override reason only when a new stakeholder is required.");
        }

        if (request.DuplicateOverrideReason is { Length: > 200 })
        {
            throw new DomainRuleException("Duplicate override reason cannot exceed 200 characters.");
        }

        var aggregate = new PatientAggregate(patient, stakeholder);
        await repository.AddAsync(aggregate, cancellationToken);
        await repository.AddAuditEventAsync(AuditEvent.Record(
            patient.TenantId,
            branchId,
            actor.ActorId,
            duplicateIds.Count == 0 ? "Patient.Registered" : "Patient.RegisteredWithDuplicateOverride",
            nameof(Patient),
            patient.Id,
            JsonSerializer.Serialize(new
            {
                patient.PatientNumber,
                patient.StakeholderId,
                DuplicateCandidateCount = duplicateIds.Count,
                DuplicateOverrideReason = duplicateIds.Count == 0 ? null : request.DuplicateOverrideReason?.Trim()
            }),
            correlationContext.CorrelationId,
            now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    public async Task<IReadOnlyCollection<PatientSearchResponse>> SearchAsync(
        long branchId,
        string query,
        CancellationToken cancellationToken)
    {
        RequireBranchPermission(FoundationPermissions.PatientsSearch, branchId);
        _ = await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
        var normalized = query.Trim().ToUpperInvariant();
        if (normalized.Length < 2)
        {
            throw new DomainRuleException("Patient search requires at least two characters.");
        }

        var results = await repository.SearchAsync(normalized, 25, cancellationToken);
        return results.Select(MapSearch).ToArray();
    }

    public async Task<PatientResponse> GetAsync(
        long branchId,
        long patientId,
        CancellationToken cancellationToken)
    {
        RequireBranchPermission(FoundationPermissions.PatientsView, branchId);
        _ = await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
        var patient = await repository.GetAsync(patientId, cancellationToken)
            ?? throw new NotFoundException("Patient was not found in the current tenant scope.");
        return Map(patient);
    }

    public async Task<PatientResponse> UpdateDemographicsAsync(
        long branchId,
        long patientId,
        UpdatePatientDemographicsRequest request,
        CancellationToken cancellationToken)
    {
        RequireBranchPermission(FoundationPermissions.PatientsUpdate, branchId);
        _ = await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
        var aggregate = await repository.GetAsync(patientId, cancellationToken)
            ?? throw new NotFoundException("Patient was not found in the current tenant scope.");
        var person = aggregate.Stakeholder.Person
            ?? throw new DomainRuleException("A patient must reference a person stakeholder.");
        var oldPatientVersion = aggregate.Patient.Version;
        var oldStakeholderVersion = aggregate.Stakeholder.Stakeholder.Version;
        var oldPersonVersion = person.Version;
        var now = clock.UtcNow;
        person.Update(
            request.ExpectedPersonVersion,
            request.Person.Honorific,
            request.Person.GivenName,
            request.Person.MiddleName,
            request.Person.FamilyName,
            request.Person.DateOfBirth,
            request.Person.IsDateOfBirthEstimated,
            ParseEnum<AdministrativeSex>(request.Person.AdministrativeSex, "Administrative sex"),
            now);
        aggregate.Stakeholder.Stakeholder.Rename(request.ExpectedStakeholderVersion, person.DisplayName, now);
        aggregate.Patient.UpdateClinicalProfile(request.ExpectedPatientVersion, request.BloodGroup, now);

        await repository.AddAuditEventAsync(AuditEvent.Record(
            aggregate.Patient.TenantId,
            branchId,
            actor.ActorId,
            "Patient.PersonProfileUpdated",
            nameof(Patient),
            patientId,
            JsonSerializer.Serialize(new
            {
                OldPatientVersion = oldPatientVersion,
                NewPatientVersion = aggregate.Patient.Version,
                OldStakeholderVersion = oldStakeholderVersion,
                NewStakeholderVersion = aggregate.Stakeholder.Stakeholder.Version,
                OldPersonVersion = oldPersonVersion,
                NewPersonVersion = person.Version
            }),
            correlationContext.CorrelationId,
            now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(aggregate);
    }

    private void RequireBranchPermission(string permission, long branchId)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
        {
            throw new ForbiddenException("The actor is not authorized for this patient operation.");
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, string label) where TEnum : struct, Enum
    {
        if (!Enum.TryParse(value, true, out TEnum parsed) || !Enum.IsDefined(parsed))
        {
            throw new DomainRuleException($"{label} is not supported.");
        }

        return parsed;
    }

    private PatientResponse Map(PatientAggregate aggregate)
    {
        var tenantId = aggregate.Patient.TenantId;
        var stakeholder = StakeholderAggregateFactory.Map(aggregate.Stakeholder, publicIds);
        return new PatientResponse(
            publicIds.Encode(PublicIdKind.Patient, aggregate.Patient.Id, tenantId),
            publicIds.Encode(PublicIdKind.Stakeholder, aggregate.Patient.StakeholderId, tenantId),
            publicIds.Encode(PublicIdKind.Branch, aggregate.Patient.RegistrationBranchId, tenantId),
            aggregate.Patient.PatientNumber,
            aggregate.Patient.Status.ToString(),
            aggregate.Patient.BloodGroup,
            stakeholder.Person!,
            stakeholder.Contacts,
            stakeholder.Identifiers,
            stakeholder.Addresses,
            stakeholder.Documents,
            stakeholder.Version,
            aggregate.Patient.Version);
    }

    private PatientSearchResponse MapSearch(PatientAggregate aggregate)
    {
        var person = aggregate.Stakeholder.Person!;
        var mobile = aggregate.Stakeholder.Contacts
            .FirstOrDefault(contact => contact.Type == ContactPointType.Mobile && contact.IsPrimary)
            ?? aggregate.Stakeholder.Contacts.FirstOrDefault(contact => contact.Type == ContactPointType.Mobile);
        var email = aggregate.Stakeholder.Contacts
            .FirstOrDefault(contact => contact.Type == ContactPointType.Email && contact.IsPrimary)
            ?? aggregate.Stakeholder.Contacts.FirstOrDefault(contact => contact.Type == ContactPointType.Email);
        return new PatientSearchResponse(
            publicIds.Encode(PublicIdKind.Patient, aggregate.Patient.Id, aggregate.Patient.TenantId),
            publicIds.Encode(PublicIdKind.Stakeholder, aggregate.Patient.StakeholderId, aggregate.Patient.TenantId),
            aggregate.Patient.PatientNumber,
            person.DisplayName,
            person.DateOfBirth?.Year,
            person.AdministrativeSex.ToString(),
            mobile is null ? null : MaskMobile(mobile.NormalizedValue),
            email is null ? null : MaskEmail(email.NormalizedValue),
            aggregate.Patient.Status.ToString());
    }

    private static string MaskMobile(string value) =>
        value.Length <= 4 ? new string('*', value.Length) : $"{new string('*', value.Length - 4)}{value[^4..]}";

    private static string MaskEmail(string value)
    {
        var separator = value.IndexOf('@', StringComparison.Ordinal);
        return separator <= 0 ? "***" : $"{value[0]}***{value[separator..]}";
    }
}
