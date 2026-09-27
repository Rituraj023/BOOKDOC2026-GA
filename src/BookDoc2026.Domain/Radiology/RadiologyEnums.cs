namespace BookDoc2026.Domain.Radiology;

public enum RadiologyStudyStatus : byte
{
    Registered = 1,
    InProgress = 2,
    Acquired = 3,
    QualityAccepted = 4,
    RepeatRequired = 5,
    Aborted = 6,
    Cancelled = 7
}

public enum RadiologyAcquisitionOutcome : byte
{
    Acquired = 1,
    Aborted = 2
}

public enum RadiologyQualityDecision : byte
{
    Accepted = 1,
    RepeatRequired = 2
}
