using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Stakeholders;

namespace BookDoc2026.UnitTests;

public sealed class StakeholderPatientDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Patient_IsAHealthcareRoleReferencingPersonStakeholder()
    {
        var stakeholder = Stakeholder.CreatePerson(NumericId.Next(), "Rituraj Kumar", Now);
        var person = StakeholderPerson.Create(
            stakeholder.TenantId,
            stakeholder.Id,
            "Mr",
            "Rituraj",
            null,
            "Kumar",
            new DateOnly(1990, 1, 2),
            false,
            AdministrativeSex.Male,
            Now);
        var patient = Patient.Register(
            stakeholder.TenantId,
            NumericId.Next(),
            Guid.NewGuid(),
            "ABCDEF",
            stakeholder.Id,
            "o+",
            Now);

        Assert.Equal(stakeholder.Id, patient.StakeholderId);
        Assert.Equal("RITURAJ KUMAR", person.NormalizedSearchName);
        Assert.Equal("O+", patient.BloodGroup);
        Assert.StartsWith("PAT-", patient.PatientNumber);
    }

    [Fact]
    public void PersonProfile_RejectsStaleVersionAndFutureBirthDate()
    {
        var stakeholder = Stakeholder.CreatePerson(NumericId.Next(), "Test Person", Now);
        var person = StakeholderPerson.Create(
            stakeholder.TenantId,
            stakeholder.Id,
            null,
            "Test",
            null,
            "Person",
            null,
            false,
            AdministrativeSex.Unknown,
            Now);

        Assert.Throws<ConcurrencyConflictException>(() => person.Update(
            2, null, "A", null, "Person", null, false, AdministrativeSex.Unknown, Now));
        Assert.Throws<DomainRuleException>(() => person.Update(
            1, null, "A", null, "Person", new DateOnly(2027, 1, 1), false, AdministrativeSex.Unknown, Now));
    }

    [Theory]
    [InlineData("+91 98765 43210", "919876543210")]
    [InlineData("9876543210", "9876543210")]
    public void StakeholderContact_NormalizesIndianCompatibleNumbers(string value, string expected)
    {
        var contact = StakeholderContactPoint.Create(
            NumericId.Next(),
            NumericId.Next(),
            ContactPointType.Mobile,
            value,
            true,
            Now);

        Assert.Equal(expected, contact.NormalizedValue);
    }

    [Fact]
    public void StakeholderAddress_RejectsInvalidIndianPostalCode()
    {
        Assert.Throws<DomainRuleException>(() => StakeholderAddress.Create(
            NumericId.Next(),
            NumericId.Next(),
            "PRIMARY",
            "Line 1",
            null,
            "Delhi",
            "DL",
            "1100",
            true,
            Now));
    }

    [Fact]
    public void CorporateStakeholder_IsNotAPatientOrPersonProfile()
    {
        var stakeholder = Stakeholder.CreateCorporate(NumericId.Next(), "Example Diagnostics Pvt Ltd", Now);
        var corporate = StakeholderCorporate.Create(
            stakeholder.TenantId,
            stakeholder.Id,
            "Example Diagnostics Private Limited",
            "Example Diagnostics",
            "CIN-EXAMPLE",
            Now);

        Assert.Equal(StakeholderType.Corporate, stakeholder.Type);
        Assert.Equal(stakeholder.Id, corporate.StakeholderId);
    }
}
