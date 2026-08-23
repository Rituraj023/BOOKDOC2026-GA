using BookDoc2026.Blazor.UI.Dashboard;

namespace BookDoc2026.UnitTests;

public sealed class PermissionDashboardCatalogTests
{
    [Fact]
    public void Filters_cards_by_all_and_any_permission_rules_without_role_names()
    {
        var cards = new[]
        {
            new DashboardCardDescriptor("reception", "Reception", "Check in", "/check-in", ["Patients.Search", "Queues.CheckIn"]),
            new DashboardCardDescriptor("messages", "Messages", "Manage", "/messages", RequiredAny: ["Templates.View", "Deliveries.View"]),
            new DashboardCardDescriptor("platform", "Platform", "Approve", "/platform", ["Tenants.Approve"])
        };

        var visible = PermissionDashboardCatalog.Filter(cards,
            ["Patients.Search", "Queues.CheckIn", "Deliveries.View"]);

        Assert.Equal(["reception", "messages"], visible.Select(card => card.Key));
    }

    [Fact]
    public void Returns_no_cards_when_required_capabilities_are_absent()
    {
        var card = new DashboardCardDescriptor("queue", "Queue", "Worklist", "/queue", ["Queues.View"]);
        Assert.Empty(PermissionDashboardCatalog.Filter([card], ["Patients.View"]));
    }
}
