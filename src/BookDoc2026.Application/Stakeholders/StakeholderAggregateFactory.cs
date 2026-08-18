using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Stakeholders;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Stakeholders;

namespace BookDoc2026.Application.Stakeholders;

internal static class StakeholderAggregateFactory
{
    public static StakeholderAggregate CreatePerson(
        long tenantId,
        StakeholderPersonRequest personRequest,
        IReadOnlyCollection<StakeholderContactRequest> contactRequests,
        IReadOnlyCollection<StakeholderIdentifierRequest> identifierRequests,
        IReadOnlyCollection<StakeholderAddressRequest> addressRequests,
        IReadOnlyCollection<StakeholderDocumentRequest> documentRequests,
        IPublicIdCodec publicIds,
        DateTimeOffset now)
    {
        var displayName = string.Join(' ', new[]
        {
            personRequest.GivenName,
            personRequest.MiddleName,
            personRequest.FamilyName
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var stakeholder = Stakeholder.CreatePerson(tenantId, displayName, now);
        var person = StakeholderPerson.Create(
            tenantId,
            stakeholder.Id,
            personRequest.Honorific,
            personRequest.GivenName,
            personRequest.MiddleName,
            personRequest.FamilyName,
            personRequest.DateOfBirth,
            personRequest.IsDateOfBirthEstimated,
            ParseEnum<AdministrativeSex>(personRequest.AdministrativeSex, "Administrative sex"),
            now);
        return CreateAggregate(
            stakeholder,
            person,
            null,
            contactRequests,
            identifierRequests,
            addressRequests,
            documentRequests,
            publicIds,
            now);
    }

    public static StakeholderAggregate CreateCorporate(
        long tenantId,
        StakeholderCorporateRequest request,
        IPublicIdCodec publicIds,
        DateTimeOffset now)
    {
        var displayName = string.IsNullOrWhiteSpace(request.TradeName) ? request.LegalName : request.TradeName;
        var stakeholder = Stakeholder.CreateCorporate(tenantId, displayName, now);
        var corporate = StakeholderCorporate.Create(
            tenantId,
            stakeholder.Id,
            request.LegalName,
            request.TradeName,
            request.RegistrationNumber,
            now);
        return CreateAggregate(
            stakeholder,
            null,
            corporate,
            request.Contacts,
            request.Identifiers,
            request.Addresses,
            request.Documents,
            publicIds,
            now);
    }

    public static StakeholderResponse Map(StakeholderAggregate aggregate, IPublicIdCodec publicIds) =>
        new(
            publicIds.Encode(PublicIdKind.Stakeholder, aggregate.Stakeholder.Id, aggregate.Stakeholder.TenantId),
            aggregate.Stakeholder.Type.ToString(),
            aggregate.Stakeholder.DisplayName,
            aggregate.Stakeholder.Status.ToString(),
            aggregate.Person is null
                ? null
                : new StakeholderPersonResponse(
                    aggregate.Person.Honorific,
                    aggregate.Person.GivenName,
                    aggregate.Person.MiddleName,
                    aggregate.Person.FamilyName,
                    aggregate.Person.DateOfBirth,
                    aggregate.Person.IsDateOfBirthEstimated,
                    aggregate.Person.AdministrativeSex.ToString(),
                    aggregate.Person.Version),
            aggregate.Corporate is null
                ? null
                : new StakeholderCorporateResponse(
                    aggregate.Corporate.LegalName,
                    aggregate.Corporate.TradeName,
                    aggregate.Corporate.RegistrationNumber,
                    aggregate.Corporate.Version),
            aggregate.Contacts.Select(contact => new StakeholderContactResponse(
                publicIds.Encode(PublicIdKind.StakeholderContact, contact.Id, contact.TenantId),
                contact.Type.ToString(),
                contact.Value,
                contact.IsPrimary,
                contact.IsVerified)).ToArray(),
            aggregate.Identifiers.Select(identifier => new StakeholderIdentifierResponse(
                publicIds.Encode(PublicIdKind.StakeholderIdentifier, identifier.Id, identifier.TenantId),
                identifier.Type,
                identifier.Value,
                identifier.Issuer,
                identifier.IsActive)).ToArray(),
            aggregate.Addresses.Select(address => new StakeholderAddressResponse(
                publicIds.Encode(PublicIdKind.StakeholderAddress, address.Id, address.TenantId),
                address.AddressType,
                address.Line1,
                address.Line2,
                address.City,
                address.StateCode,
                address.PostalCode,
                address.CountryCode,
                address.IsPrimary)).ToArray(),
            aggregate.Documents.Select(document => new StakeholderDocumentResponse(
                publicIds.Encode(PublicIdKind.StakeholderDocument, document.Id, document.TenantId),
                publicIds.Encode(PublicIdKind.DocumentType, document.DocumentTypeId, document.TenantId),
                publicIds.Encode(PublicIdKind.StoredFile, document.FileId, document.TenantId),
                document.ReferenceNumber,
                document.IssuedOn,
                document.ExpiresOn,
                document.IsActive)).ToArray(),
            aggregate.Stakeholder.Version);

    private static StakeholderAggregate CreateAggregate(
        Stakeholder stakeholder,
        StakeholderPerson? person,
        StakeholderCorporate? corporate,
        IReadOnlyCollection<StakeholderContactRequest> contactRequests,
        IReadOnlyCollection<StakeholderIdentifierRequest> identifierRequests,
        IReadOnlyCollection<StakeholderAddressRequest> addressRequests,
        IReadOnlyCollection<StakeholderDocumentRequest> documentRequests,
        IPublicIdCodec publicIds,
        DateTimeOffset now)
    {
        var contacts = contactRequests.Select(contact => StakeholderContactPoint.Create(
            stakeholder.TenantId,
            stakeholder.Id,
            ParseEnum<ContactPointType>(contact.Type, "Contact type"),
            contact.Value,
            contact.IsPrimary,
            now)).ToArray();
        var identifiers = identifierRequests.Select(identifier => StakeholderIdentifier.Create(
            stakeholder.TenantId,
            stakeholder.Id,
            identifier.Type,
            identifier.Value,
            identifier.Issuer,
            now)).ToArray();
        var addresses = addressRequests.Select(address => StakeholderAddress.Create(
            stakeholder.TenantId,
            stakeholder.Id,
            address.AddressType,
            address.Line1,
            address.Line2,
            address.City,
            address.StateCode,
            address.PostalCode,
            address.IsPrimary,
            now)).ToArray();
        var documents = documentRequests.Select(document => StakeholderDocumentReference.Create(
            stakeholder.TenantId,
            stakeholder.Id,
            publicIds.Decode(PublicIdKind.DocumentType, document.DocumentTypeId, stakeholder.TenantId),
            publicIds.Decode(PublicIdKind.StoredFile, document.FileId, stakeholder.TenantId),
            document.ReferenceNumber,
            document.IssuedOn,
            document.ExpiresOn,
            now)).ToArray();
        Validate(contacts, identifiers, addresses);
        return new StakeholderAggregate(stakeholder, person, corporate, contacts, identifiers, addresses, documents);
    }

    private static void Validate(
        IReadOnlyCollection<StakeholderContactPoint> contacts,
        IReadOnlyCollection<StakeholderIdentifier> identifiers,
        IReadOnlyCollection<StakeholderAddress> addresses)
    {
        if (contacts.GroupBy(contact => contact.Type).Any(group => group.Count(contact => contact.IsPrimary) > 1))
        {
            throw new DomainRuleException("Only one primary contact is allowed for each contact type.");
        }

        if (contacts.GroupBy(contact => new { contact.Type, contact.NormalizedValue }).Any(group => group.Count() > 1))
        {
            throw new DomainRuleException("Duplicate stakeholder contacts are not allowed.");
        }

        if (identifiers.GroupBy(identifier => new
        {
            identifier.Type,
            identifier.Issuer,
            identifier.NormalizedValue
        }).Any(group => group.Count() > 1))
        {
            throw new DomainRuleException("Duplicate stakeholder identifiers are not allowed.");
        }

        if (addresses.Count(address => address.IsPrimary) > 1)
        {
            throw new DomainRuleException("Only one primary stakeholder address is allowed.");
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
}
