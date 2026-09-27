using System.Data;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Queues;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class QueueRepository(BookDocDbContext dbContext) : IQueueRepository
{
    private static readonly SemaphoreSlim InMemoryTicketGate = new(1, 1);

    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(branch => branch.Id == branchId, cancellationToken);

    public Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken) =>
        dbContext.Patients.AnyAsync(patient => patient.Id == patientId, cancellationToken);

    public Task<bool> ResourceExistsAsync(long branchId, long resourceId, CancellationToken cancellationToken) =>
        dbContext.BookableResources.AnyAsync(resource => resource.BranchId == branchId && resource.Id == resourceId,
            cancellationToken);

    public Task<bool> BookingMatchesAsync(long branchId, long bookingId, long patientId, CancellationToken cancellationToken) =>
        dbContext.Bookings.AnyAsync(booking => booking.BranchId == branchId && booking.Id == bookingId
            && booking.PatientId == patientId, cancellationToken);

    public Task<bool> ServicePointCodeExistsAsync(long branchId, string code, CancellationToken cancellationToken) =>
        dbContext.ImagingServicePoints.AnyAsync(point => point.BranchId == branchId && point.Code == code,
            cancellationToken);

    public Task<ImagingServicePoint?> GetServicePointAsync(long branchId, long servicePointId, CancellationToken cancellationToken) =>
        dbContext.ImagingServicePoints.AsNoTracking().SingleOrDefaultAsync(
            point => point.BranchId == branchId && point.Id == servicePointId, cancellationToken);

    public async Task<IReadOnlyCollection<ImagingServicePoint>> ListServicePointsAsync(
        long branchId,
        CancellationToken cancellationToken) =>
        await dbContext.ImagingServicePoints.AsNoTracking()
            .Where(point => point.BranchId == branchId)
            .OrderByDescending(point => point.IsActive)
            .ThenBy(point => point.Name)
            .ThenBy(point => point.Code)
            .ToListAsync(cancellationToken);

    public async Task AddServicePointAsync(
        ImagingServicePoint servicePoint,
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.ImagingServicePoints.AddAsync(servicePoint, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public async Task<QueueTicketCreationResult> CreateTicketAsync(
        QueueTicket ticket,
        QueueTicketEvent queueEvent,
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            await InMemoryTicketGate.WaitAsync(cancellationToken);
            try { return await CreateTicketCoreAsync(ticket, queueEvent, auditEvent, cancellationToken); }
            finally { InMemoryTicketGate.Release(); }
        }

        if (dbContext.Database.CurrentTransaction is not null)
            return await CreateRelationalTicketCoreAsync(ticket, queueEvent, auditEvent, cancellationToken);
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await CreateRelationalTicketCoreAsync(ticket, queueEvent, auditEvent, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task<QueueTicketCreationResult> CreateRelationalTicketCoreAsync(
        QueueTicket ticket,
        QueueTicketEvent queueEvent,
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_getapplock @Resource={"BOOKDOC:QUEUE:CHECKIN:" + ticket.TenantId + ":" + ticket.RequestId}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000",
            cancellationToken);
        return await CreateTicketCoreAsync(ticket, queueEvent, auditEvent, cancellationToken);
    }

    private async Task<QueueTicketCreationResult> CreateTicketCoreAsync(
        QueueTicket ticket,
        QueueTicketEvent queueEvent,
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.QueueTickets.SingleOrDefaultAsync(
            candidate => candidate.RequestId == ticket.RequestId, cancellationToken);
        if (existing is not null)
        {
            if (existing.BranchId != ticket.BranchId || existing.ServicePointId != ticket.ServicePointId
                || existing.PatientId != ticket.PatientId || existing.BookingId != ticket.BookingId
                || existing.InvestigationOrderId != ticket.InvestigationOrderId
                || existing.Priority != ticket.Priority)
                throw new DomainRuleException("The queue check-in request identifier was reused with different content.");
            return new(existing, true);
        }

        await dbContext.QueueTickets.AddAsync(ticket, cancellationToken);
        await dbContext.QueueTicketEvents.AddAsync(queueEvent, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return new(ticket, false);
    }

    public async Task<QueueTicket?> GetTicketAsync(
        long branchId,
        long ticketId,
        bool tracked,
        CancellationToken cancellationToken)
    {
        var query = dbContext.QueueTickets.Where(ticket => ticket.BranchId == branchId && ticket.Id == ticketId);
        return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<QueueTicket>> ListTicketsAsync(
        long branchId,
        long servicePointId,
        CancellationToken cancellationToken) =>
        await dbContext.QueueTickets.AsNoTracking()
            .Where(ticket => ticket.BranchId == branchId && ticket.ServicePointId == servicePointId)
            .OrderBy(ticket => ticket.Status == QueueTicketStatus.Completed || ticket.Status == QueueTicketStatus.Cancelled)
            .ThenByDescending(ticket => ticket.Priority)
            .ThenBy(ticket => ticket.ArrivedUtc)
            .ThenBy(ticket => ticket.Id)
            .ToListAsync(cancellationToken);

    public async Task AddTransitionEvidenceAsync(
        QueueTicketEvent queueEvent,
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.QueueTicketEvents.AddAsync(queueEvent, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("The queue record changed while it was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Queue data conflicts with an existing request or relationship.");
        }
    }
}
