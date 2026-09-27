namespace BookDoc2026.Domain.Clinical;

public enum EncounterStatus : byte
{
    Draft = 1,
    Signed = 2
}

public enum EncounterRevisionKind : byte
{
    Draft = 1,
    Signed = 2,
    Amendment = 3
}

public enum InvestigationOrderStatus : byte
{
    Requested = 1
}

public enum InvestigationResultStatus : byte
{
    Pending = 1
}
