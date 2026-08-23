using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Contracts;

public sealed class ContractAgreement : TenantScopedEntity
{
    private ContractAgreement() { }

    public long BranchId { get; private set; }
    public long PatientId { get; private set; }
    public string ContractNumber { get; private set; } = string.Empty;
    public string ContractTypeCode { get; private set; } = string.Empty;
    public DateOnly ValidFrom { get; private set; }
    public DateOnly ValidTo { get; private set; }
    public ContractStatus Status { get; private set; }
    public decimal PackagePrice { get; private set; }
    public string Currency { get; private set; } = "INR";
    public string RuleVersion { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public long Version { get; private set; } = 1;

    public static ContractAgreement Create(
        long tenantId,
        long branchId,
        long patientId,
        string contractNumber,
        string contractTypeCode,
        DateOnly validFrom,
        DateOnly validTo,
        decimal packagePrice,
        string currency,
        string ruleVersion,
        string? notes,
        DateTimeOffset now)
    {
        if (tenantId <= 0 || branchId <= 0 || patientId <= 0)
            throw new DomainRuleException("Contract scope and patient are required.");
        if (validTo < validFrom)
            throw new DomainRuleException("Contract validity end cannot precede its start.");
        var agreement = new ContractAgreement
        {
            TenantId = tenantId,
            BranchId = branchId,
            PatientId = patientId,
            ContractNumber = Normalize(contractNumber, 8, 40, "Contract number"),
            ContractTypeCode = NormalizeCode(contractTypeCode, 3, 40, "Contract type code"),
            ValidFrom = validFrom,
            ValidTo = validTo,
            Status = ContractStatus.Active,
            PackagePrice = Money(packagePrice, "Package price"),
            Currency = NormalizeCode(currency, 3, 3, "Currency"),
            RuleVersion = Normalize(ruleVersion, 1, 40, "Rule version"),
            Notes = Optional(notes, 1000)
        };
        agreement.StampCreated(now);
        return agreement;
    }

    public bool IsEffective(DateOnly serviceDate) =>
        Status == ContractStatus.Active && serviceDate >= ValidFrom && serviceDate <= ValidTo;

    internal static decimal Money(decimal value, string label)
    {
        if (value < 0 || value > 999_999_999.99m || decimal.Round(value, 2) != value)
            throw new DomainRuleException($"{label} must be a non-negative amount with at most two decimal places.");
        return value;
    }

    internal static string Normalize(string value, int min, int max, string label)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length < min || normalized.Length > max)
            throw new DomainRuleException($"{label} must contain between {min} and {max} characters.");
        return normalized;
    }

    internal static string NormalizeCode(string value, int min, int max, string label) =>
        Normalize(value, min, max, label).ToUpperInvariant();

    internal static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > max) throw new DomainRuleException($"Text cannot exceed {max} characters.");
        return normalized;
    }
}
