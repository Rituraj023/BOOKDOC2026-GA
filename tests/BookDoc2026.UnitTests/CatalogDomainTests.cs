using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.UnitTests;

public sealed class CatalogDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ExclusiveResource_RequiresCapacityOne()
    {
        Assert.Throws<DomainRuleException>(() => BookableResource.Create(
            NumericId.Next(),
            NumericId.Next(),
            NumericId.Next(),
            "CT-01",
            "CT Scanner 1",
            CapacityMode.Exclusive,
            2,
            "Asia/Kolkata",
            null,
            Now));
    }

    [Fact]
    public void InactiveResource_CannotBecomeAvailable()
    {
        var resource = NewResource();
        resource.Update(1, "CT Scanner", CapacityMode.Exclusive, 1, false, Now.AddMinutes(1));

        Assert.Throws<DomainRuleException>(() => resource.ChangeStatus(
            2,
            ResourceOperationalStatus.Available,
            Now.AddMinutes(2)));
    }

    [Fact]
    public void ResourceStatus_RequiresCurrentVersion()
    {
        var resource = NewResource();

        Assert.Throws<ConcurrencyConflictException>(() => resource.ChangeStatus(
            99,
            ResourceOperationalStatus.Maintenance,
            Now));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(1441)]
    public void Service_RejectsUnsupportedDuration(int minutes)
    {
        Assert.Throws<DomainRuleException>(() => ClinicalService.Create(
            NumericId.Next(), "CT-SCAN", "CT Scan", null, minutes, Now));
    }

    private static BookableResource NewResource() => BookableResource.Create(
        NumericId.Next(),
        NumericId.Next(),
        NumericId.Next(),
        "CT-01",
        "CT Scanner 1",
        CapacityMode.Exclusive,
        1,
        "Asia/Kolkata",
        null,
        Now);
}
