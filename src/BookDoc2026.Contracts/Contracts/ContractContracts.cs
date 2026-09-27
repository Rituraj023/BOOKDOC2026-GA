namespace BookDoc2026.Contracts.Contracts;

public sealed record CreateContractEntitlementRequest(
    string ServiceId,
    string? ResourceCategoryId,
    int TotalUnits,
    decimal UnitPrice,
    string RuleVersion);

public sealed record CreateContractRequest(
    string PatientId,
    string ContractNumber,
    string ContractTypeCode,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    decimal PackagePrice,
    string Currency,
    string RuleVersion,
    string? Notes,
    IReadOnlyCollection<CreateContractEntitlementRequest> Entitlements);

public sealed record ContractEntitlementResponse(
    string Id,
    string ServiceId,
    string? ResourceCategoryId,
    int TotalUnits,
    int ReservedUnits,
    int ConsumedUnits,
    int AvailableUnits,
    decimal UnitPrice,
    string Currency,
    string RuleVersion,
    long Version);

public sealed record ContractResponse(
    string Id,
    string PatientId,
    string ContractNumber,
    string ContractTypeCode,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string Status,
    decimal PackagePrice,
    string Currency,
    string RuleVersion,
    string? Notes,
    long Version,
    IReadOnlyCollection<ContractEntitlementResponse> Entitlements);

public sealed record ReserveEntitlementRequest(
    Guid RequestId,
    string BookingId,
    int Units,
    long ExpectedEntitlementVersion);

public sealed record ConsumeEntitlementReservationRequest(
    long ExpectedReservationVersion,
    long ExpectedEntitlementVersion);

public sealed record ReleaseEntitlementReservationRequest(
    long ExpectedReservationVersion,
    long ExpectedEntitlementVersion,
    string Reason);

public sealed record EntitlementReservationResponse(
    string Id,
    string ContractId,
    string EntitlementId,
    string BookingId,
    Guid RequestId,
    int Units,
    string Status,
    DateTimeOffset ReservedUtc,
    DateTimeOffset? ConsumedUtc,
    DateTimeOffset? ReleasedUtc,
    string? ReleaseReason,
    long Version,
    ContractEntitlementResponse Entitlement,
    bool IsReplay);
public sealed record PatientPackageSummaryResponse(
    string ContractId,
    string ContractNumber,
    string ContractTypeCode,
    DateOnly ValidTo,
    string EntitlementId,
    string ServiceId,
    string? ResourceCategoryId,
    int AvailableUnits,
    int TotalUnits,
    int ConsumedUnits,
    decimal UnitPrice,
    string Currency)
{
    public int BalanceVisits => AvailableUnits;
    public string ContractCode => ContractNumber;
    public string Description => $"{ContractTypeCode} Package ({AvailableUnits} visits remaining)";
    public DateTimeOffset? ExpiresUtc => new DateTimeOffset(ValidTo.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
}

public sealed record BookingPackageStatusResponse(
    string BookingId,
    bool IsLinkedToPackage,
    string? ReservationId,
    string? ContractId,
    string? ContractNumber,
    string? EntitlementId,
    int? Units,
    string? Status,
    int? BalanceRemaining = null,
    int? ConsumedUnits = null)
{
    public string? ContractCode => ContractNumber;
}

public sealed record LinkBookingPackageRequest(
    Guid RequestId,
    string ContractId,
    string EntitlementId,
    int Units = 1,
    long ExpectedEntitlementVersion = 0);

public sealed record UnlinkBookingPackageRequest(
    long ExpectedReservationVersion = 0,
    long ExpectedEntitlementVersion = 0,
    string Reason = "Unlinked from booking");
