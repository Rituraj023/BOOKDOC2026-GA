namespace BookDoc2026.Blazor.UI.Clinical;

public sealed record InvestigationQueueHandoffSelection(
    string OrderId,
    string ServicePointId,
    string Priority,
    string? PriorityReason);
