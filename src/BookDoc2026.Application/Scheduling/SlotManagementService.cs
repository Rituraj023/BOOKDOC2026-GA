using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Scheduling;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Scheduling;

public sealed class SlotManagementService(
    ISchedulingRepository repository,
    IPractitionerRepository practitionerRepository,
    ICatalogRepository catalogRepository,
    IPublicIdCodec codec,
    ICurrentActor currentActor,
    IClock clock) : ISlotManagementService
{
    public async Task<IReadOnlyCollection<DoctorSlotResponse>> GenerateSlotsAsync(
        GenerateDoctorSlotsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = EnsureTenantId();

        var branchId = codec.Decode(PublicIdKind.Branch, request.BranchId, tenantId);
        var practitionerId = codec.Decode(PublicIdKind.Practitioner, request.PractitionerId, tenantId);
        var serviceId = codec.DecodeOptional(PublicIdKind.ClinicalService, request.ServiceId, tenantId);

        var branch = await repository.GetBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch not found.");

        if (request.EndDate < request.StartDate)
            throw new DomainRuleException("End date cannot be earlier than start date.");

        if (request.ShiftEnd <= request.ShiftStart)
            throw new DomainRuleException("Shift end time must be after shift start time.");

        if (request.SlotDurationMinutes is < 5 or > 240)
            throw new DomainRuleException("Slot duration must be between 5 and 240 minutes.");

        if (request.MaxCapacityPerSlot < 1)
            throw new DomainRuleException("Slot capacity must be at least 1.");

        var timeZone = ResolveTimeZone(branch.TimeZoneId);
        var now = clock.UtcNow;
        var createdSlots = new List<BookingSlot>();

        var days = request.DaysOfWeek?.Count > 0
            ? new HashSet<DayOfWeek>(request.DaysOfWeek)
            : new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            if (!days.Contains(date.DayOfWeek))
                continue;

            var currentStart = request.ShiftStart;
            while (currentStart.AddMinutes(request.SlotDurationMinutes) <= request.ShiftEnd)
            {
                var currentEnd = currentStart.AddMinutes(request.SlotDurationMinutes);

                // Exclude break windows
                var overlapsBreak = request.Breaks?.Any(b =>
                    currentStart < b.EndTime && currentEnd > b.StartTime) ?? false;

                if (!overlapsBreak)
                {
                    var localStartDt = date.ToDateTime(currentStart);
                    var localEndDt = date.ToDateTime(currentEnd);
                    var startUtc = new DateTimeOffset(localStartDt, timeZone.GetUtcOffset(localStartDt));
                    var endUtc = new DateTimeOffset(localEndDt, timeZone.GetUtcOffset(localEndDt));

                    var overlapsExisting = await repository.SlotOverlapsAsync(
                        branchId, practitionerId, startUtc, endUtc, cancellationToken);

                    if (!overlapsExisting)
                    {
                        var slot = BookingSlot.Create(
                            tenantId,
                            branchId,
                            practitionerId,
                            serviceId,
                            date,
                            startUtc,
                            endUtc,
                            request.SlotDurationMinutes,
                            request.MaxCapacityPerSlot,
                            now);

                        createdSlots.Add(slot);
                    }
                }

                currentStart = currentEnd;
            }
        }

        if (createdSlots.Count > 0)
        {
            await repository.AddSlotsAsync(createdSlots, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }

        // Return all slots for the specified range
        return await GetSlotsAsync(
            request.BranchId,
            request.PractitionerId,
            request.ServiceId,
            request.StartDate,
            request.EndDate,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<DoctorSlotResponse>> GetSlotsAsync(
        string branchId,
        string? practitionerId,
        string? serviceId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var practitionerInternalId = codec.DecodeOptional(PublicIdKind.Practitioner, practitionerId, tenantId);
        var serviceInternalId = codec.DecodeOptional(PublicIdKind.ClinicalService, serviceId, tenantId);

        var slots = await repository.ListSlotsAsync(
            branchInternalId,
            practitionerInternalId,
            serviceInternalId,
            fromDate,
            toDate,
            cancellationToken);

        return await MapSlotsToResponsesAsync(branchInternalId, slots, tenantId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DoctorSlotResponse>> GetAvailableSlotsAsync(
        string branchId,
        string? practitionerId,
        string? serviceId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var practitionerInternalId = codec.DecodeOptional(PublicIdKind.Practitioner, practitionerId, tenantId);
        var serviceInternalId = codec.DecodeOptional(PublicIdKind.ClinicalService, serviceId, tenantId);

        var slots = await repository.ListAvailableSlotsAsync(
            branchInternalId,
            practitionerInternalId,
            serviceInternalId,
            fromDate,
            toDate,
            cancellationToken);

        return await MapSlotsToResponsesAsync(branchInternalId, slots, tenantId, cancellationToken);
    }

    public async Task<DoctorSlotResponse> BlockSlotAsync(
        string branchId,
        string slotId,
        BlockDoctorSlotRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var slotInternalId = codec.Decode(PublicIdKind.BookingSlot, slotId, tenantId);

        var slot = await repository.GetSlotAsync(branchInternalId, slotInternalId, tracked: true, cancellationToken)
            ?? throw new NotFoundException("Doctor slot not found.");

        slot.Block(request.Reason, clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        var mapped = await MapSlotsToResponsesAsync(branchInternalId, new List<BookingSlot> { slot }, tenantId, cancellationToken);
        return mapped.First();
    }

    public async Task<DoctorSlotResponse> UnblockSlotAsync(
        string branchId,
        string slotId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var slotInternalId = codec.Decode(PublicIdKind.BookingSlot, slotId, tenantId);

        var slot = await repository.GetSlotAsync(branchInternalId, slotInternalId, tracked: true, cancellationToken)
            ?? throw new NotFoundException("Doctor slot not found.");

        slot.Unblock(clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        var mapped = await MapSlotsToResponsesAsync(branchInternalId, new List<BookingSlot> { slot }, tenantId, cancellationToken);
        return mapped.First();
    }

    public async Task<DoctorSlotResponse> CancelSlotAsync(
        string branchId,
        string slotId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var slotInternalId = codec.Decode(PublicIdKind.BookingSlot, slotId, tenantId);

        var slot = await repository.GetSlotAsync(branchInternalId, slotInternalId, tracked: true, cancellationToken)
            ?? throw new NotFoundException("Doctor slot not found.");

        slot.Cancel(clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        var mapped = await MapSlotsToResponsesAsync(branchInternalId, new List<BookingSlot> { slot }, tenantId, cancellationToken);
        return mapped.First();
    }

    public async Task<DirectSlotBookingConfirmationResponse> BookSlotDirectAsync(
        string branchId,
        BookSlotDirectRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = EnsureTenantId();
        var branchInternalId = codec.Decode(PublicIdKind.Branch, branchId, tenantId);
        var slotInternalId = codec.Decode(PublicIdKind.BookingSlot, request.SlotId, tenantId);

        var slot = await repository.GetSlotAsync(branchInternalId, slotInternalId, tracked: true, cancellationToken)
            ?? throw new NotFoundException("Requested doctor slot not found.");

        var now = clock.UtcNow;
        slot.ReserveCapacity(1, now);
        await repository.SaveChangesAsync(cancellationToken);

        // Fetch practitioner name for confirmation
        var roster = await practitionerRepository.ListBranchPractitionersAsync(branchInternalId, cancellationToken);
        var practitionerName = roster.FirstOrDefault(p => p.Profile.Id == slot.PractitionerId)?.DisplayName ?? "Attending Doctor";

        var tokenCode = $"TKN-{Math.Abs(slot.Id ^ now.Ticks) % 900000 + 100000}";
        var bookingNumber = $"BKG-{slot.SlotDate:yyyyMMdd}-{slot.Id % 10000:D4}";
        var publicBookingId = codec.Encode(PublicIdKind.BookingSlot, slot.Id, tenantId);

        return new DirectSlotBookingConfirmationResponse(
            BookingId: publicBookingId,
            BookingNumber: bookingNumber,
            SlotId: request.SlotId,
            PractitionerName: practitionerName,
            StartUtc: slot.StartUtc,
            EndUtc: slot.EndUtc,
            PatientFullName: request.PatientFullName.Trim(),
            Status: "Confirmed",
            TokenCode: tokenCode);
    }

    private async Task<IReadOnlyCollection<DoctorSlotResponse>> MapSlotsToResponsesAsync(
        long branchId,
        IEnumerable<BookingSlot> slots,
        long tenantId,
        CancellationToken cancellationToken)
    {
        var roster = await practitionerRepository.ListBranchPractitionersAsync(branchId, cancellationToken);
        var practitionerMap = roster.ToDictionary(r => r.Profile.Id, r => r.DisplayName);

        var responses = new List<DoctorSlotResponse>();
        foreach (var s in slots)
        {
            practitionerMap.TryGetValue(s.PractitionerId, out var practitionerName);
            string? serviceName = null;
            if (s.ServiceId.HasValue)
            {
                var svc = await catalogRepository.GetServiceAsync(s.ServiceId.Value, cancellationToken);
                serviceName = svc?.Name;
            }

            responses.Add(new DoctorSlotResponse(
                Id: codec.Encode(PublicIdKind.BookingSlot, s.Id, tenantId),
                BranchId: codec.Encode(PublicIdKind.Branch, s.BranchId, tenantId),
                PractitionerId: codec.Encode(PublicIdKind.Practitioner, s.PractitionerId, tenantId),
                PractitionerName: practitionerName,
                ServiceId: codec.EncodeOptional(PublicIdKind.ClinicalService, s.ServiceId, tenantId),
                ServiceName: serviceName,
                SlotDate: s.SlotDate,
                StartUtc: s.StartUtc,
                EndUtc: s.EndUtc,
                DurationMinutes: s.DurationMinutes,
                MaxCapacity: s.MaxCapacity,
                BookedCount: s.BookedCount,
                AvailableCapacity: Math.Max(0, s.MaxCapacity - s.BookedCount),
                Status: s.Status.ToString(),
                BlockReason: s.BlockReason,
                Version: s.Version));
        }

        return responses;
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;

        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return TimeZoneInfo.Utc; }
    }

    private long EnsureTenantId()
    {
        if (!currentActor.TenantId.HasValue || currentActor.TenantId.Value <= 0)
            throw new DomainRuleException("A valid tenant context is required.");
        return currentActor.TenantId.Value;
    }
}
