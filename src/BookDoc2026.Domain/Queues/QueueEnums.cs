namespace BookDoc2026.Domain.Queues;

public enum ImagingModality
{
    XRay = 1,
    CT = 2
}

public enum QueuePriority
{
    Normal = 1,
    Urgent = 2
}

public enum QueueTicketStatus
{
    Waiting = 1,
    Called = 2,
    Preparation = 3,
    InService = 4,
    Completed = 5,
    Cancelled = 6
}
