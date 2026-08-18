using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Stakeholders;

public sealed class StakeholderPerson : TenantScopedEntity
{
    private StakeholderPerson()
    {
    }

    public long StakeholderId { get; private set; }

    public string? Honorific { get; private set; }

    public string GivenName { get; private set; } = string.Empty;

    public string? MiddleName { get; private set; }

    public string FamilyName { get; private set; } = string.Empty;

    public string NormalizedSearchName { get; private set; } = string.Empty;

    public DateOnly? DateOfBirth { get; private set; }

    public bool IsDateOfBirthEstimated { get; private set; }

    public AdministrativeSex AdministrativeSex { get; private set; }

    public long Version { get; private set; } = 1;

    public string DisplayName => string.Join(' ', new[] { GivenName, MiddleName, FamilyName }
        .Where(value => !string.IsNullOrWhiteSpace(value)));

    public static StakeholderPerson Create(
        long tenantId,
        long stakeholderId,
        string? honorific,
        string givenName,
        string? middleName,
        string familyName,
        DateOnly? dateOfBirth,
        bool isDateOfBirthEstimated,
        AdministrativeSex administrativeSex,
        DateTimeOffset now)
    {
        Validate(givenName, familyName, dateOfBirth, now);
        var person = new StakeholderPerson
        {
            TenantId = tenantId,
            StakeholderId = stakeholderId,
            Honorific = CleanOptional(honorific),
            GivenName = givenName.Trim(),
            MiddleName = CleanOptional(middleName),
            FamilyName = familyName.Trim(),
            DateOfBirth = dateOfBirth,
            IsDateOfBirthEstimated = dateOfBirth.HasValue && isDateOfBirthEstimated,
            AdministrativeSex = administrativeSex
        };
        person.NormalizedSearchName = NormalizeName(person.GivenName, person.MiddleName, person.FamilyName);
        person.StampCreated(now);
        return person;
    }

    public void Update(
        long expectedVersion,
        string? honorific,
        string givenName,
        string? middleName,
        string familyName,
        DateOnly? dateOfBirth,
        bool isDateOfBirthEstimated,
        AdministrativeSex administrativeSex,
        DateTimeOffset now)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException("The person profile changed after it was loaded.");
        }

        Validate(givenName, familyName, dateOfBirth, now);
        Honorific = CleanOptional(honorific);
        GivenName = givenName.Trim();
        MiddleName = CleanOptional(middleName);
        FamilyName = familyName.Trim();
        NormalizedSearchName = NormalizeName(GivenName, MiddleName, FamilyName);
        DateOfBirth = dateOfBirth;
        IsDateOfBirthEstimated = dateOfBirth.HasValue && isDateOfBirthEstimated;
        AdministrativeSex = administrativeSex;
        Version++;
        StampModified(now);
    }

    public static string NormalizeName(params string?[] values) =>
        string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()))
            .ToUpperInvariant();

    private static void Validate(string givenName, string familyName, DateOnly? dateOfBirth, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(givenName) || string.IsNullOrWhiteSpace(familyName))
        {
            throw new DomainRuleException("Given name and family name are required.");
        }

        if (dateOfBirth > DateOnly.FromDateTime(now.UtcDateTime))
        {
            throw new DomainRuleException("Date of birth cannot be in the future.");
        }
    }

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
