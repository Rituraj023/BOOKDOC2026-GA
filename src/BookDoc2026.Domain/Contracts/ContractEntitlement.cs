using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Contracts;

public sealed class ContractEntitlement : TenantScopedEntity
{
    private ContractEntitlement() { }

    public long BranchId { get; private set; }
    public long ContractId { get; private set; }
    public long ServiceId { get; private set; }
    public long? ResourceCategoryId { get; private set; }
    public int TotalUnits { get; private set; }
    public int ReservedUnits { get; private set; }
    public int ConsumedUnits { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = "INR";
    public string RuleVersion { get; private set; } = string.Empty;
    public long Version { get; private set; } = 1;
    public int AvailableUnits => TotalUnits - ReservedUnits - ConsumedUnits;

    public static ContractEntitlement Create(
        ContractAgreement agreement,
        long serviceId,
        long? resourceCategoryId,
        int totalUnits,
        decimal unitPrice,
        string currency,
        string ruleVersion,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(agreement);
        if (serviceId <= 0 || resourceCategoryId is <= 0)
            throw new DomainRuleException("A valid service and optional resource category are required.");
        if (totalUnits is < 1 or > 100_000)
            throw new DomainRuleException("Entitlement units must be between 1 and 100000.");
        var entitlement = new ContractEntitlement
        {
            TenantId = agreement.TenantId,
            BranchId = agreement.BranchId,
            ContractId = agreement.Id,
            ServiceId = serviceId,
            ResourceCategoryId = resourceCategoryId,
            TotalUnits = totalUnits,
            UnitPrice = ContractAgreement.Money(unitPrice, "Unit price"),
            Currency = ContractAgreement.NormalizeCode(currency, 3, 3, "Currency"),
            RuleVersion = ContractAgreement.Normalize(ruleVersion, 1, 40, "Rule version")
        };
        entitlement.StampCreated(now);
        return entitlement;
    }

    public void Reserve(int units, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        ValidateUnits(units);
        if (units > AvailableUnits) throw new DomainRuleException("The contract does not have enough available entitlement units.");
        ReservedUnits += units;
        Advance(now);
    }

    public void ConsumeReserved(int units, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        ValidateUnits(units);
        if (units > ReservedUnits) throw new DomainRuleException("Reserved entitlement units are insufficient for consumption.");
        ReservedUnits -= units;
        ConsumedUnits += units;
        Advance(now);
    }

    public void ReleaseReserved(int units, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        ValidateUnits(units);
        if (units > ReservedUnits) throw new DomainRuleException("Reserved entitlement units are insufficient for release.");
        ReservedUnits -= units;
        Advance(now);
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ConcurrencyConflictException("The entitlement changed after it was loaded.");
    }

    private static void ValidateUnits(int units)
    {
        if (units is < 1 or > 100_000) throw new DomainRuleException("Entitlement units must be positive.");
    }

    private void Advance(DateTimeOffset now)
    {
        Version++;
        StampModified(now);
    }
}
