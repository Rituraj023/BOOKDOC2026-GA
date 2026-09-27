using BookDoc2026.Contracts.Clinical;

namespace BookDoc2026.Blazor.UI.Clinical;

public sealed record PhysiotherapyOutcomeSeries(
    string MeasureCode,
    string ToolVersion,
    string Unit,
    string? BodySite,
    string? LateralityCode,
    IReadOnlyList<PhysiotherapyOutcomeResponse> Observations);

public static class PhysiotherapyOutcomeSeriesBuilder
{
    public static IReadOnlyList<PhysiotherapyOutcomeSeries> Build(
        IEnumerable<PhysiotherapyOutcomeResponse> observations) => observations
        .GroupBy(item => new
        {
            Measure = item.MeasureCode.Trim().ToUpperInvariant(),
            Tool = item.ToolVersion.Trim(),
            Unit = item.Unit.Trim(),
            Body = item.BodySite?.Trim().ToUpperInvariant(),
            Side = item.LateralityCode?.Trim().ToUpperInvariant()
        })
        .Select(group => new PhysiotherapyOutcomeSeries(group.Key.Measure, group.Key.Tool, group.Key.Unit,
            group.Key.Body, group.Key.Side, group.OrderBy(item => item.ObservedUtc).ToArray()))
        .OrderBy(series => series.MeasureCode, StringComparer.Ordinal)
        .ThenBy(series => series.BodySite, StringComparer.Ordinal)
        .ThenBy(series => series.LateralityCode, StringComparer.Ordinal)
        .ToArray();
}
