using System.Security.Cryptography;
using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Queues;

public sealed class QueueTicket : TenantScopedEntity
{
    private QueueTicket() { }

    public long BranchId { get; private set; }
    public long ServicePointId { get; private set; }
    public long PatientId { get; private set; }
    public long? BookingId { get; private set; }
    public long? InvestigationOrderId { get; private set; }
    public Guid RequestId { get; private set; }
    public string DisplayToken { get; private set; } = string.Empty;
    public QueuePriority Priority { get; private set; }
    public QueueTicketStatus Status { get; private set; }
    public DateTimeOffset ArrivedUtc { get; private set; }
    public DateTimeOffset? CalledUtc { get; private set; }
    public DateTimeOffset? ServiceStartedUtc { get; private set; }
    public DateTimeOffset? CompletedUtc { get; private set; }
    public DateTimeOffset? CancelledUtc { get; private set; }
    public int CallCount { get; private set; }
    public long Version { get; private set; } = 1;

    public static QueueTicket CheckIn(
        long tenantId,
        long branchId,
        long servicePointId,
        long patientId,
        long? bookingId,
        Guid requestId,
        QueuePriority priority,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0 || servicePointId <= 0 || patientId <= 0
            || requestId == Guid.Empty || !Enum.IsDefined(priority))
            throw new DomainRuleException("Queue ticket scope, patient or priority is invalid.");
        return Create(tenantId, branchId, servicePointId, patientId, bookingId, null,
            requestId, priority, now);
    }

    public static QueueTicket CheckInInvestigation(
        long tenantId,
        long branchId,
        long servicePointId,
        long patientId,
        long investigationOrderId,
        Guid requestId,
        QueuePriority priority,
        DateTimeOffset now)
    {
        if (investigationOrderId <= 0)
            throw new DomainRuleException("An investigation order is required for this queue handoff.");
        return Create(tenantId, branchId, servicePointId, patientId, null, investigationOrderId,
            requestId, priority, now);
    }

    private static QueueTicket Create(
        long tenantId,
        long branchId,
        long servicePointId,
        long patientId,
        long? bookingId,
        long? investigationOrderId,
        Guid requestId,
        QueuePriority priority,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0 || servicePointId <= 0 || patientId <= 0
            || requestId == Guid.Empty || !Enum.IsDefined(priority))
            throw new DomainRuleException("Queue ticket scope, patient or priority is invalid.");
        var ticket = new QueueTicket
        {
            TenantId = tenantId,
            BranchId = branchId,
            ServicePointId = servicePointId,
            PatientId = patientId,
            BookingId = bookingId,
            InvestigationOrderId = investigationOrderId,
            RequestId = requestId,
            Priority = priority,
            Status = QueueTicketStatus.Waiting,
            ArrivedUtc = now
        };
        ticket.DisplayToken = CreateDisplayToken(ticket.Id);
        ticket.StampCreated(now);
        return ticket;
    }

    public void Call(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(QueueTicketStatus.Waiting, "Only a waiting ticket can be called.");
        Status = QueueTicketStatus.Called;
        CalledUtc = now;
        CallCount++;
        Advance(now);
    }

    public void Recall(long expectedVersion, string reason, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(QueueTicketStatus.Called, "Only a called ticket can be recalled.");
        _ = NormalizeReason(reason);
        CalledUtc = now;
        CallCount++;
        Advance(now);
    }

    public void BeginPreparation(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(QueueTicketStatus.Called, "Only a called ticket can enter preparation.");
        Status = QueueTicketStatus.Preparation;
        Advance(now);
    }

    public void StartService(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(QueueTicketStatus.Preparation, "Only a prepared ticket can start imaging.");
        Status = QueueTicketStatus.InService;
        ServiceStartedUtc = now;
        Advance(now);
    }

    public void Complete(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(QueueTicketStatus.InService, "Only an in-service ticket can complete.");
        Status = QueueTicketStatus.Completed;
        CompletedUtc = now;
        Advance(now);
    }

    public void Cancel(long expectedVersion, string reason, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status is QueueTicketStatus.Completed or QueueTicketStatus.Cancelled)
            throw new DomainRuleException("A terminal queue ticket cannot be cancelled.");
        _ = NormalizeReason(reason);
        Status = QueueTicketStatus.Cancelled;
        CancelledUtc = now;
        Advance(now);
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The queue ticket changed after it was loaded.");
    }

    private void EnsureStatus(QueueTicketStatus required, string message)
    {
        if (Status != required) throw new DomainRuleException(message);
    }

    private void Advance(DateTimeOffset now)
    {
        Version++;
        StampModified(now);
    }

    public static string NormalizeReason(string reason)
    {
        var normalized = reason?.Trim() ?? string.Empty;
        if (normalized.Length is < 3 or > 250)
            throw new DomainRuleException("A queue reason between 3 and 250 characters is required.");
        return normalized;
    }

    private static string CreateDisplayToken(long id)
    {
        var digest = SHA256.HashData(BitConverter.GetBytes(id));
        return $"Q-{Convert.ToHexString(digest)[..8]}";
    }
}
