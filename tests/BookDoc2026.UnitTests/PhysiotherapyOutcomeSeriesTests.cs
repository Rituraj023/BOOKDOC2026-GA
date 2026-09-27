using BookDoc2026.Blazor.UI.Clinical;
using BookDoc2026.Contracts.Clinical;

namespace BookDoc2026.UnitTests;

public sealed class PhysiotherapyOutcomeSeriesTests
{
    [Fact]
    public void Groups_only_like_for_like_outcomes_and_orders_observations_chronologically()
    {
        var later = Outcome("outcome-2", "NPRS", "1.0", 4, "score", "Knee", "Right",
            new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero));
        var earlier = Outcome("outcome-1", "nprs", "1.0", 7, "score", "knee", "right",
            new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero));
        var otherSide = Outcome("outcome-3", "NPRS", "1.0", 3, "score", "Knee", "Left",
            new DateTimeOffset(2026, 8, 21, 10, 0, 0, TimeSpan.Zero));
        var newerTool = Outcome("outcome-4", "NPRS", "2.0", 2, "score", "Knee", "Right",
            new DateTimeOffset(2026, 8, 22, 10, 0, 0, TimeSpan.Zero));

        var series = PhysiotherapyOutcomeSeriesBuilder.Build([later, otherSide, newerTool, earlier]);

        Assert.Equal(3, series.Count);
        var comparable = Assert.Single(series, item => item.ToolVersion == "1.0" && item.LateralityCode == "RIGHT");
        Assert.Equal(["outcome-1", "outcome-2"], comparable.Observations.Select(item => item.Id));
    }

    private static PhysiotherapyOutcomeResponse Outcome(string id, string measure, string tool, decimal value,
        string unit, string body, string side, DateTimeOffset observedUtc) =>
        new(id, null, "REASSESSMENT", measure, tool, value, unit, body, side, observedUtc, "actor-1");
}
