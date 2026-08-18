using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Scheduling;

public sealed class SchedulingHold : TenantScopedEntity
{
    private SchedulingHold() { }

    public long BranchId { get; private set; }
    public long PatientId { get; private set; }
    public long ServiceId { get; private set; }
    public Guid RequestId { get; private set; }
    public string PayloadHash { get; private set; } = string.Empty;
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public DateTimeOffset ExpiresUtc { get; private set; }
    public SchedulingHoldStatus Status { get; private set; }
    public long Version { get; private set; } = 1;

    public static SchedulingHold Create(long tenantId, long branchId, long patientId, long serviceId,
        Guid requestId, string payloadHash, DateTimeOffset startUtc, DateTimeOffset endUtc,
        DateTimeOffset expiresUtc, DateTimeOffset now)
    {
        if (requestId == Guid.Empty || startUtc >= endUtc || startUtc <= now || expiresUtc <= now || expiresUtc > now.AddMinutes(30)
            || payloadHash.Length != 64)
            throw new DomainRuleException("Scheduling hold timing, request identifier, or payload hash is invalid.");

        var hold = new SchedulingHold
        {
            TenantId = tenantId,
            BranchId = branchId,
            PatientId = patientId,
            ServiceId = serviceId,
            RequestId = requestId,
            PayloadHash = payloadHash,
            StartUtc = startUtc,
            EndUtc = endUtc,
            ExpiresUtc = expiresUtc,
            Status = SchedulingHoldStatus.Active
        };
        hold.StampCreated(now);
        return hold;
    }

    public void Release(long expectedVersion, DateTimeOffset now)
    {
        if (Version != expectedVersion) throw new ConcurrencyConflictException("The hold changed after it was loaded.");
        if (Status != SchedulingHoldStatus.Active) throw new DomainRuleException("Only an active hold can be released.");
        Status = SchedulingHoldStatus.Released;
        Version++;
        StampModified(now);
    }

    public void MarkExpired(DateTimeOffset now)
    {
        if (Status == SchedulingHoldStatus.Active && ExpiresUtc <= now)
        {
            Status = SchedulingHoldStatus.Expired;
            Version++;
            StampModified(now);
        }
    }
}
