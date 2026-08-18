namespace BookDoc2026.Contracts.Stakeholders;

public sealed record StakeholderPersonRequest(
    string? Honorific,
    string GivenName,
    string? MiddleName,
    string FamilyName,
    DateOnly? DateOfBirth,
    bool IsDateOfBirthEstimated,
    string AdministrativeSex);

public sealed record CreatePersonStakeholderRequest(
    StakeholderPersonRequest Person,
    IReadOnlyCollection<StakeholderContactRequest> Contacts,
    IReadOnlyCollection<StakeholderIdentifierRequest> Identifiers,
    IReadOnlyCollection<StakeholderAddressRequest> Addresses,
    IReadOnlyCollection<StakeholderDocumentRequest> Documents);

public sealed record StakeholderCorporateRequest(
    string LegalName,
    string? TradeName,
    string? RegistrationNumber,
    IReadOnlyCollection<StakeholderContactRequest> Contacts,
    IReadOnlyCollection<StakeholderIdentifierRequest> Identifiers,
    IReadOnlyCollection<StakeholderAddressRequest> Addresses,
    IReadOnlyCollection<StakeholderDocumentRequest> Documents);

public sealed record StakeholderContactRequest(string Type, string Value, bool IsPrimary);

public sealed record StakeholderIdentifierRequest(string Type, string Value, string? Issuer);

public sealed record StakeholderAddressRequest(
    string AddressType,
    string Line1,
    string? Line2,
    string City,
    string StateCode,
    string PostalCode,
    bool IsPrimary);

public sealed record StakeholderDocumentRequest(
    string DocumentTypeId,
    string FileId,
    string? ReferenceNumber,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn);

public sealed record StakeholderResponse(
    string Id,
    string Type,
    string DisplayName,
    string Status,
    StakeholderPersonResponse? Person,
    StakeholderCorporateResponse? Corporate,
    IReadOnlyCollection<StakeholderContactResponse> Contacts,
    IReadOnlyCollection<StakeholderIdentifierResponse> Identifiers,
    IReadOnlyCollection<StakeholderAddressResponse> Addresses,
    IReadOnlyCollection<StakeholderDocumentResponse> Documents,
    long Version);

public sealed record StakeholderPersonResponse(
    string? Honorific,
    string GivenName,
    string? MiddleName,
    string FamilyName,
    DateOnly? DateOfBirth,
    bool IsDateOfBirthEstimated,
    string AdministrativeSex,
    long Version);

public sealed record StakeholderCorporateResponse(
    string LegalName,
    string? TradeName,
    string? RegistrationNumber,
    long Version);

public sealed record StakeholderContactResponse(
    string Id,
    string Type,
    string Value,
    bool IsPrimary,
    bool IsVerified);

public sealed record StakeholderIdentifierResponse(
    string Id,
    string Type,
    string Value,
    string Issuer,
    bool IsActive);

public sealed record StakeholderAddressResponse(
    string Id,
    string AddressType,
    string Line1,
    string? Line2,
    string City,
    string StateCode,
    string PostalCode,
    string CountryCode,
    bool IsPrimary);

public sealed record StakeholderDocumentResponse(
    string Id,
    string DocumentTypeId,
    string FileId,
    string? ReferenceNumber,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    bool IsActive);
