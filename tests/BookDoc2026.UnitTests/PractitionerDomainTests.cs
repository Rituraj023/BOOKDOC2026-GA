using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Workforce;

namespace BookDoc2026.UnitTests;

public sealed class PractitionerDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Practitioner_CannotActivateWithoutCurrentVerifiedCredential()
    {
        var practitioner = PractitionerProfile.Create(1, 2, 3, "PHY-001", "PHYSIOTHERAPIST", Now);
        Assert.Throws<DomainRuleException>(() => practitioner.Activate(1, false, Now));
        practitioner.Activate(1, true, Now);
        Assert.Equal(PractitionerStatus.Active, practitioner.Status);
        Assert.Equal(2, practitioner.Version);
    }

    [Fact]
    public void CredentialDecision_IsFinalAndValidityIsDateBound()
    {
        var credential = PractitionerCredential.Create(1, 2, "DPT", "DL-PT-1001",
            "Delhi Council", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Now);
        credential.Verify(1, 99, Now);
        Assert.True(credential.IsCurrent(new DateOnly(2026, 8, 23)));
        Assert.False(credential.IsCurrent(new DateOnly(2027, 1, 1)));
        Assert.Throws<DomainRuleException>(() => credential.Reject(2, 99, "Changed decision", Now));
    }

    [Fact]
    public void Assignment_IsEffectiveOnlyWhileActiveAndWithinDates()
    {
        var assignment = PractitionerAssignment.Create(1, 2, 3, 4, null, "TREATING",
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), Now);
        Assert.True(assignment.IsEffective(new DateOnly(2026, 8, 23)));
        Assert.False(assignment.IsEffective(new DateOnly(2026, 9, 1)));
        assignment.Suspend(1, Now);
        Assert.False(assignment.IsEffective(new DateOnly(2026, 8, 23)));
    }

    [Fact]
    public void PractitionerAndAssignment_RejectStaleVersions()
    {
        var practitioner = PractitionerProfile.Create(1, 2, 3, "ORTHO-001", "ORTHOPAEDIC", Now);
        practitioner.Activate(1, true, Now);
        Assert.Throws<ConcurrencyConflictException>(() => practitioner.Suspend(1, Now));
        var assignment = PractitionerAssignment.Create(1, 2, 3, 4, null, "CONSULTING",
            new DateOnly(2026, 1, 1), null, Now);
        assignment.End(1, Now);
        Assert.Throws<ConcurrencyConflictException>(() => assignment.End(1, Now));
    }
}
