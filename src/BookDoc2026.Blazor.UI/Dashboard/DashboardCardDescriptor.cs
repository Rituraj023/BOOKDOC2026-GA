namespace BookDoc2026.Blazor.UI.Dashboard;

public sealed record DashboardCardDescriptor(
    string Key,
    string Title,
    string Description,
    string Href,
    IReadOnlyCollection<string>? RequiredAll = null,
    IReadOnlyCollection<string>? RequiredAny = null,
    string? Badge = null);

public static class PermissionDashboardCatalog
{
    public static IReadOnlyList<DashboardCardDescriptor> Filter(
        IEnumerable<DashboardCardDescriptor> cards,
        IEnumerable<string> permissions)
    {
        var granted = permissions.ToHashSet(StringComparer.Ordinal);
        return cards.Where(card =>
                (card.RequiredAll is null || card.RequiredAll.All(granted.Contains))
                && (card.RequiredAny is null || card.RequiredAny.Count == 0 || card.RequiredAny.Any(granted.Contains)))
            .ToArray();
    }
}
