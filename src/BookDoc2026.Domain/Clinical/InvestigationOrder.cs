using System.Security.Cryptography;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Queues;

namespace BookDoc2026.Domain.Clinical;

public sealed class InvestigationOrder : TenantScopedEntity
{
    private InvestigationOrder() { }

    public long BranchId { get; private set; }
    public long EncounterId { get; private set; }
    public long PatientId { get; private set; }
    public long RequestedServiceId { get; private set; }
    public Guid RequestId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public ImagingModality Modality { get; private set; }
    public string ClinicalIndication { get; private set; } = string.Empty;
    public InvestigationOrderStatus Status { get; private set; }
    public InvestigationResultStatus ResultStatus { get; private set; }
    public long RequestedByActorId { get; private set; }
    public DateTimeOffset OrderedUtc { get; private set; }
    public DateTimeOffset? QueueHandoffUtc { get; private set; }
    public long? QueueHandoffByActorId { get; private set; }
    public long Version { get; private set; } = 1;

    public static InvestigationOrder Request(
        ClinicalEncounter encounter,
        long requestedServiceId,
        ImagingModality modality,
        string clinicalIndication,
        Guid requestId,
        long actorId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        if (encounter.Status != EncounterStatus.Signed)
            throw new DomainRuleException("A signed encounter is required to request an investigation.");
        if (requestedServiceId <= 0 || requestId == Guid.Empty || actorId <= 0 || !Enum.IsDefined(modality))
            throw new DomainRuleException("Investigation service, modality, request identity or author is invalid.");

        var order = new InvestigationOrder
        {
            TenantId = encounter.TenantId,
            BranchId = encounter.BranchId,
            EncounterId = encounter.Id,
            PatientId = encounter.PatientId,
            RequestedServiceId = requestedServiceId,
            RequestId = requestId,
            Modality = modality,
            ClinicalIndication = NormalizeIndication(clinicalIndication),
            Status = InvestigationOrderStatus.Requested,
            ResultStatus = InvestigationResultStatus.Pending,
            RequestedByActorId = actorId,
            OrderedUtc = now
        };
        order.OrderNumber = CreateNumber(requestId);
        order.StampCreated(now);
        return order;
    }

    public void RecordQueueHandoff(long expectedVersion, long actorId, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (actorId <= 0) throw new DomainRuleException("A queue handoff actor is required.");
        if (QueueHandoffUtc.HasValue)
            throw new DomainRuleException("This investigation order already has a queue handoff.");

        QueueHandoffUtc = now;
        QueueHandoffByActorId = actorId;
        Version++;
        StampModified(now);
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The investigation order changed after it was loaded.");
    }

    private static string NormalizeIndication(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 3 or > 2000)
            throw new DomainRuleException("A clinical indication between 3 and 2000 characters is required.");
        return normalized;
    }

    private static string CreateNumber(Guid requestId)
    {
        var digest = SHA256.HashData(requestId.ToByteArray());
        return $"INV-{Convert.ToHexString(digest)[..16]}";
    }
}
