using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Queues;

namespace BookDoc2026.Application.Abstractions;

public sealed record QueueTicketCreationResult(QueueTicket Ticket, bool IsReplay);

public interface IQueueRepository
{
    Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken);
    Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken);
    Task<bool> ResourceExistsAsync(long branchId, long resourceId, CancellationToken cancellationToken);
    Task<bool> BookingMatchesAsync(long branchId, long bookingId, long patientId, CancellationToken cancellationToken);
    Task<bool> ServicePointCodeExistsAsync(long branchId, string code, CancellationToken cancellationToken);
    Task<ImagingServicePoint?> GetServicePointAsync(long branchId, long servicePointId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ImagingServicePoint>> ListServicePointsAsync(long branchId, CancellationToken cancellationToken);
    Task AddServicePointAsync(ImagingServicePoint servicePoint, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<QueueTicketCreationResult> CreateTicketAsync(QueueTicket ticket, QueueTicketEvent queueEvent, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<QueueTicket?> GetTicketAsync(long branchId, long ticketId, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<QueueTicket>> ListTicketsAsync(long branchId, long servicePointId, CancellationToken cancellationToken);
    Task AddTransitionEvidenceAsync(QueueTicketEvent queueEvent, AuditEvent auditEvent, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record QueueChangedNotification(
    long TenantId,
    long BranchId,
    long ServicePointId,
    long TicketId,
    QueueTicketStatus Status,
    long Version,
    DateTimeOffset OccurredUtc);

public interface IQueueRealtimeNotifier
{
    Task NotifyAsync(QueueChangedNotification notification, CancellationToken cancellationToken);
}
