using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class PatientRepository(BookDocDbContext dbContext) : IPatientRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public async Task<PatientAggregate?> GetByRegistrationRequestAsync(
        Guid registrationRequestId,
        CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients.SingleOrDefaultAsync(
            candidate => candidate.RegistrationRequestId == registrationRequestId,
            cancellationToken);
        return patient is null ? null : await LoadAggregateAsync(patient, cancellationToken);
    }

    public async Task<PatientAggregate?> GetAsync(long patientId, CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients.SingleOrDefaultAsync(candidate => candidate.Id == patientId, cancellationToken);
        return patient is null ? null : await LoadAggregateAsync(patient, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PatientAggregate>> SearchAsync(
        string normalizedQuery,
        int limit,
        CancellationToken cancellationToken)
    {
        var lowercaseQuery = normalizedQuery.ToLowerInvariant();
        var patients = await dbContext.Patients
            .Where(patient => patient.PatientNumber.StartsWith(normalizedQuery)
                || dbContext.StakeholderPersons.Any(person => person.StakeholderId == patient.StakeholderId
                    && person.NormalizedSearchName.StartsWith(normalizedQuery))
                || dbContext.StakeholderContactPoints.Any(contact => contact.StakeholderId == patient.StakeholderId
                    && (contact.NormalizedValue.StartsWith(normalizedQuery)
                        || contact.NormalizedValue.StartsWith(lowercaseQuery)))
                || dbContext.StakeholderIdentifiers.Any(identifier => identifier.StakeholderId == patient.StakeholderId
                    && identifier.IsActive
                    && identifier.NormalizedValue.StartsWith(normalizedQuery)))
            .OrderBy(patient => patient.PatientNumber)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return await LoadAggregatesAsync(patients, cancellationToken);
    }

    public async Task<IReadOnlyCollection<long>> FindPotentialDuplicatesAsync(
        string normalizedName,
        DateOnly? dateOfBirth,
        IReadOnlyCollection<string> normalizedContacts,
        IReadOnlyCollection<(string Type, string Issuer, string Value)> normalizedIdentifiers,
        CancellationToken cancellationToken)
    {
        var stakeholderIds = new HashSet<long>();
        if (dateOfBirth.HasValue)
        {
            stakeholderIds.UnionWith(await dbContext.StakeholderPersons
                .Where(person => person.NormalizedSearchName == normalizedName && person.DateOfBirth == dateOfBirth)
                .Select(person => person.StakeholderId)
                .ToListAsync(cancellationToken));
        }

        if (normalizedContacts.Count > 0)
        {
            stakeholderIds.UnionWith(await dbContext.StakeholderContactPoints
                .Where(contact => normalizedContacts.Contains(contact.NormalizedValue))
                .Select(contact => contact.StakeholderId)
                .Distinct()
                .ToListAsync(cancellationToken));
        }

        var identifierValues = normalizedIdentifiers.Select(identifier => identifier.Value).Distinct().ToArray();
        if (identifierValues.Length > 0)
        {
            var possibleIdentifierMatches = await dbContext.StakeholderIdentifiers
                .Where(identifier => identifier.IsActive && identifierValues.Contains(identifier.NormalizedValue))
                .Select(identifier => new
                {
                    identifier.StakeholderId,
                    identifier.Type,
                    identifier.Issuer,
                    Value = identifier.NormalizedValue
                })
                .ToListAsync(cancellationToken);
            var exactIdentifiers = normalizedIdentifiers.ToHashSet();
            stakeholderIds.UnionWith(possibleIdentifierMatches
                .Where(identifier => exactIdentifiers.Contains((identifier.Type, identifier.Issuer, identifier.Value)))
                .Select(identifier => identifier.StakeholderId));
        }

        if (stakeholderIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Patients
            .Where(patient => stakeholderIds.Contains(patient.StakeholderId))
            .Select(patient => patient.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PatientAggregate aggregate, CancellationToken cancellationToken)
    {
        await AddStakeholderGraphAsync(aggregate.Stakeholder, cancellationToken);
        await dbContext.Patients.AddAsync(aggregate.Patient, cancellationToken);
    }

    public Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken).AsTask();

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("The patient or stakeholder changed while the operation was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Patient/stakeholder data conflicts with an existing record.");
        }
    }

    private async Task AddStakeholderGraphAsync(
        StakeholderAggregate aggregate,
        CancellationToken cancellationToken)
    {
        await dbContext.Stakeholders.AddAsync(aggregate.Stakeholder, cancellationToken);
        if (aggregate.Person is not null)
        {
            await dbContext.StakeholderPersons.AddAsync(aggregate.Person, cancellationToken);
        }

        if (aggregate.Corporate is not null)
        {
            await dbContext.StakeholderCorporates.AddAsync(aggregate.Corporate, cancellationToken);
        }

        await dbContext.StakeholderContactPoints.AddRangeAsync(aggregate.Contacts, cancellationToken);
        await dbContext.StakeholderIdentifiers.AddRangeAsync(aggregate.Identifiers, cancellationToken);
        await dbContext.StakeholderAddresses.AddRangeAsync(aggregate.Addresses, cancellationToken);
        await dbContext.StakeholderDocumentReferences.AddRangeAsync(aggregate.Documents, cancellationToken);
    }

    private async Task<PatientAggregate> LoadAggregateAsync(Patient patient, CancellationToken cancellationToken) =>
        new(patient, await LoadStakeholderAsync(patient.StakeholderId, cancellationToken));

    private async Task<IReadOnlyCollection<PatientAggregate>> LoadAggregatesAsync(
        IReadOnlyCollection<Patient> patients,
        CancellationToken cancellationToken)
    {
        var results = new List<PatientAggregate>(patients.Count);
        foreach (var patient in patients)
        {
            results.Add(await LoadAggregateAsync(patient, cancellationToken));
        }

        return results;
    }

    private async Task<StakeholderAggregate> LoadStakeholderAsync(
        long stakeholderId,
        CancellationToken cancellationToken)
    {
        var stakeholder = await dbContext.Stakeholders.SingleAsync(
            candidate => candidate.Id == stakeholderId,
            cancellationToken);
        var person = await dbContext.StakeholderPersons.SingleOrDefaultAsync(
            candidate => candidate.StakeholderId == stakeholderId,
            cancellationToken);
        var corporate = await dbContext.StakeholderCorporates.SingleOrDefaultAsync(
            candidate => candidate.StakeholderId == stakeholderId,
            cancellationToken);
        var contacts = await dbContext.StakeholderContactPoints
            .Where(contact => contact.StakeholderId == stakeholderId)
            .OrderByDescending(contact => contact.IsPrimary)
            .ThenBy(contact => contact.Type)
            .ToListAsync(cancellationToken);
        var identifiers = await dbContext.StakeholderIdentifiers
            .Where(identifier => identifier.StakeholderId == stakeholderId)
            .OrderBy(identifier => identifier.Type)
            .ToListAsync(cancellationToken);
        var addresses = await dbContext.StakeholderAddresses
            .Where(address => address.StakeholderId == stakeholderId)
            .OrderByDescending(address => address.IsPrimary)
            .ThenBy(address => address.AddressType)
            .ToListAsync(cancellationToken);
        var documents = await dbContext.StakeholderDocumentReferences
            .Where(document => document.StakeholderId == stakeholderId)
            .OrderBy(document => document.DocumentTypeId)
            .ToListAsync(cancellationToken);
        return new StakeholderAggregate(stakeholder, person, corporate, contacts, identifiers, addresses, documents);
    }
}
