using BookDoc2026.Contracts.Queues;

namespace BookDoc2026.Blazor.UI.Queues;

public sealed class RadiologyCheckInDraft
{
    public Guid RequestId { get; private set; } = Guid.NewGuid();
    public QueueTicketResponse? Result { get; private set; }
    public bool IsComplete => Result is not null;

    public void Complete(QueueTicketResponse result) => Result = result;

    public void Reset()
    {
        RequestId = Guid.NewGuid();
        Result = null;
    }
}
