using BookDoc2026.Contracts.Stakeholders;

namespace BookDoc2026.Contracts.Patients;

public sealed record RegisterPatientRequest(
    Guid RegistrationRequestId,
    StakeholderPersonRequest Person,
    string? BloodGroup,
    IReadOnlyCollection<StakeholderContactRequest> Contacts,
    IReadOnlyCollection<StakeholderIdentifierRequest> Identifiers,
    IReadOnlyCollection<StakeholderAddressRequest> Addresses,
    IReadOnlyCollection<StakeholderDocumentRequest> Documents,
    string? DuplicateOverrideReason);

public sealed record UpdatePatientDemographicsRequest(
    long ExpectedPatientVersion,
    long ExpectedStakeholderVersion,
    long ExpectedPersonVersion,
    StakeholderPersonRequest Person,
    string? BloodGroup);

public sealed record PatientResponse(
    string Id,
    string StakeholderId,
    string RegistrationBranchId,
    string PatientNumber,
    string Status,
    string? BloodGroup,
    StakeholderPersonResponse Person,
    IReadOnlyCollection<StakeholderContactResponse> Contacts,
    IReadOnlyCollection<StakeholderIdentifierResponse> Identifiers,
    IReadOnlyCollection<StakeholderAddressResponse> Addresses,
    IReadOnlyCollection<StakeholderDocumentResponse> Documents,
    long StakeholderVersion,
    long Version);

public sealed record PatientSearchResponse(
    string Id,
    string StakeholderId,
    string PatientNumber,
    string DisplayName,
    int? BirthYear,
    string AdministrativeSex,
    string? MaskedMobile,
    string? MaskedEmail,
    string Status);
