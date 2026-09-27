using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Scheduling;
using BookDoc2026.Domain.Stakeholders;
using BookDoc2026.Domain.Workforce;
using Microsoft.EntityFrameworkCore;

namespace BookDoc2026.Infrastructure.Data;

public sealed class EncounterRepository(BookDocDbContext dbContext) : IEncounterRepository
{
    public Task<Branch?> GetBranchAsync(long branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public Task<Booking?> GetBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken) =>
        dbContext.Bookings.AsNoTracking().SingleOrDefaultAsync(
            item => item.BranchId == branchId && item.Id == bookingId, cancellationToken);

    public Task<bool> EncounterExistsForBookingAsync(long branchId, long bookingId,
        CancellationToken cancellationToken) => dbContext.ClinicalEncounters.AnyAsync(
        item => item.BranchId == branchId && item.BookingId == bookingId, cancellationToken);

    public async Task<EncounterAggregate?> GetEncounterAsync(long branchId, long encounterId, bool tracked,
        CancellationToken cancellationToken)
    {
        var encounters = dbContext.ClinicalEncounters.Where(
            item => item.BranchId == branchId && item.Id == encounterId);
        var encounter = await (tracked ? encounters : encounters.AsNoTracking())
            .SingleOrDefaultAsync(cancellationToken);
        if (encounter is null) return null;
        var revisions = dbContext.EncounterRevisions.Where(item => item.EncounterId == encounterId);
        return new EncounterAggregate(encounter, await (tracked ? revisions : revisions.AsNoTracking())
            .OrderBy(item => item.RevisionNumber).ToArrayAsync(cancellationToken));
    }

    public async Task<IReadOnlyCollection<ClinicalAgendaWorkItem>> ListAgendaAsync(long branchId, long actorId,
        DateOnly localDate, DateTimeOffset startUtc, DateTimeOffset endUtc, int take,
        CancellationToken cancellationToken)
    {
        if (actorId is <= 0 or > uint.MaxValue) return [];
        var assignments = await dbContext.PractitionerAssignments.AsNoTracking()
            .Where(assignment => assignment.BranchId == branchId
                && assignment.Status == PractitionerAssignmentStatus.Active
                && assignment.EffectiveFrom <= localDate
                && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= localDate)
                && dbContext.PractitionerProfiles.Any(profile => profile.Id == assignment.PractitionerId
                    && profile.IdentitySubjectId == (uint)actorId
                    && profile.Status == PractitionerStatus.Active
                    && dbContext.Users.Any(user => user.Id == profile.IdentitySubjectId && user.IsActive)
                    && dbContext.Stakeholders.Any(stakeholder => stakeholder.Id == profile.StakeholderId
                        && stakeholder.Status == StakeholderStatus.Active)
                    && dbContext.PractitionerCredentials.Any(credential =>
                        credential.PractitionerId == profile.Id
                        && credential.VerificationStatus == CredentialVerificationStatus.Verified
                        && credential.ValidFrom <= localDate
                        && (!credential.ValidTo.HasValue || credential.ValidTo.Value >= localDate))))
            .Select(assignment => new { assignment.ServiceId, assignment.BookableResourceId })
            .ToArrayAsync(cancellationToken);
        if (assignments.Length == 0) return [];

        IQueryable<long>? authorizedBookingIds = null;
        foreach (var assignment in assignments)
        {
            var serviceId = assignment.ServiceId;
            var resourceId = assignment.BookableResourceId;
            var ids = dbContext.Bookings.Where(booking => booking.BranchId == branchId
                    && booking.Status == BookingStatus.Confirmed && booking.ServiceId == serviceId
                    && booking.StartUtc >= startUtc && booking.StartUtc < endUtc
                    && (!resourceId.HasValue || dbContext.BookingResourceAllocations.Any(allocation =>
                        allocation.BookingId == booking.Id && allocation.ResourceId == resourceId.Value)))
                .Select(booking => booking.Id);
            authorizedBookingIds = authorizedBookingIds is null ? ids : authorizedBookingIds.Concat(ids);
        }

        var bookingIds = authorizedBookingIds!.Distinct();
        var rows = await (from booking in dbContext.Bookings.AsNoTracking()
            join patient in dbContext.Patients.AsNoTracking() on booking.PatientId equals patient.Id
            join stakeholder in dbContext.Stakeholders.AsNoTracking() on patient.StakeholderId equals stakeholder.Id
            join service in dbContext.ClinicalServices.AsNoTracking() on booking.ServiceId equals service.Id
            where bookingIds.Contains(booking.Id)
            orderby booking.StartUtc, booking.BookingNumber
            select new { Booking = booking, patient.PatientNumber, PatientDisplayName = stakeholder.DisplayName,
                ServiceCode = service.Code, ServiceName = service.Name })
            .Take(take).ToArrayAsync(cancellationToken);
        if (rows.Length == 0) return [];

        var selectedBookingIds = rows.Select(row => row.Booking.Id).ToArray();
        var encounters = await dbContext.ClinicalEncounters.AsNoTracking()
            .Where(encounter => selectedBookingIds.Contains(encounter.BookingId))
            .ToDictionaryAsync(encounter => encounter.BookingId, cancellationToken);
        var patientIds = rows.Select(row => row.Booking.PatientId).Distinct().ToArray();
        var serviceIds = rows.Select(row => row.Booking.ServiceId).Distinct().ToArray();
        var plans = await dbContext.PhysiotherapyCarePlans.AsNoTracking()
            .Where(plan => plan.BranchId == branchId && patientIds.Contains(plan.PatientId)
                && serviceIds.Contains(plan.ServiceId))
            .ToArrayAsync(cancellationToken);

        return rows.Select(row =>
        {
            encounters.TryGetValue(row.Booking.Id, out var encounter);
            var plan = plans.Where(candidate => candidate.PatientId == row.Booking.PatientId
                    && candidate.ServiceId == row.Booking.ServiceId
                    && (candidate.Status is PhysiotherapyCarePlanStatus.Draft or PhysiotherapyCarePlanStatus.Active
                        || encounter is not null && candidate.InitialEncounterId == encounter.Id))
                .OrderByDescending(candidate => encounter is not null && candidate.InitialEncounterId == encounter.Id)
                .ThenByDescending(candidate => candidate.Status == PhysiotherapyCarePlanStatus.Active)
                .ThenByDescending(candidate => candidate.ModifiedUtc)
                .FirstOrDefault();
            return new ClinicalAgendaWorkItem(row.Booking, row.PatientNumber, row.PatientDisplayName,
                row.ServiceCode, row.ServiceName, encounter, plan);
        }).ToArray();
    }

    public async Task AddEncounterAsync(ClinicalEncounter encounter, EncounterRevision revision,
        AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await dbContext.ClinicalEncounters.AddAsync(encounter, cancellationToken);
        await dbContext.EncounterRevisions.AddAsync(revision, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public async Task AddRevisionAsync(EncounterRevision revision, AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.EncounterRevisions.AddAsync(revision, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public async Task AddVitalSignsAsync(PatientVitalSigns vitals, AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await dbContext.PatientVitalSigns.AddAsync(vitals, cancellationToken);
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    public Task<PatientVitalSigns?> GetLatestVitalSignsAsync(long branchId, long patientId, CancellationToken cancellationToken) =>
        dbContext.PatientVitalSigns.AsNoTracking()
            .Where(v => v.BranchId == branchId && v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<PatientVitalSigns>> ListVitalSignsAsync(long branchId, long patientId, int take, CancellationToken cancellationToken) =>
        await dbContext.PatientVitalSigns.AsNoTracking()
            .Where(v => v.BranchId == branchId && v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedUtc)
            .Take(take)
            .ToArrayAsync(cancellationToken);

    public Task<PatientVitalSigns?> GetVitalSignsByBookingAsync(long branchId, long bookingId, CancellationToken cancellationToken) =>
        dbContext.PatientVitalSigns.AsNoTracking()
            .Where(v => v.BranchId == branchId && v.BookingId == bookingId)
            .OrderByDescending(v => v.RecordedUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PatientVitalSigns?> GetVitalSignsByEncounterAsync(long branchId, long encounterId, CancellationToken cancellationToken) =>
        dbContext.PatientVitalSigns.AsNoTracking()
            .Where(v => v.BranchId == branchId && v.ClinicalEncounterId == encounterId)
            .OrderByDescending(v => v.RecordedUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Clinical encounter data changed while it was being saved.");
        }
        catch (DbUpdateException)
        {
            throw new DomainRuleException("Clinical encounter data conflicts with an existing booking or revision.");
        }
    }
}
