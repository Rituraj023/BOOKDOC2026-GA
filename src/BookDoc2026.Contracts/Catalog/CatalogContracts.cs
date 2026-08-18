namespace BookDoc2026.Contracts.Catalog;

public sealed record CreateServiceRequest(
    string Code,
    string Name,
    string? Description,
    int DefaultDurationMinutes);

public sealed record UpdateServiceRequest(
    long ExpectedVersion,
    string Name,
    string? Description,
    int DefaultDurationMinutes,
    bool IsActive);

public sealed record ServiceResponse(
    string Id,
    string Code,
    string Name,
    string? Description,
    int DefaultDurationMinutes,
    string Status,
    long Version);

public sealed record CreateResourceCategoryRequest(
    string? ParentCategoryId,
    string Code,
    string Name,
    string Kind);

public sealed record UpdateResourceCategoryRequest(long ExpectedVersion, string Name, bool IsActive);

public sealed record ResourceCategoryResponse(
    string Id,
    string? ParentCategoryId,
    string Code,
    string Name,
    string Kind,
    bool IsActive,
    long Version);

public sealed record CreateBookableResourceRequest(
    string CategoryId,
    string Code,
    string Name,
    string CapacityMode,
    int Capacity,
    string? TimeZoneId,
    string? ExternalReferenceId);

public sealed record UpdateBookableResourceRequest(
    long ExpectedVersion,
    string Name,
    string CapacityMode,
    int Capacity,
    bool IsActive);

public sealed record ChangeResourceStatusRequest(
    long ExpectedVersion,
    string Status,
    string Reason);

public sealed record BookableResourceResponse(
    string Id,
    string BranchId,
    string CategoryId,
    string CategoryCode,
    string CategoryKind,
    string Code,
    string Name,
    string CapacityMode,
    int Capacity,
    string TimeZoneId,
    string? ExternalReferenceId,
    bool IsActive,
    string OperationalStatus,
    long Version);

public sealed record AddResourceCapabilityRequest(
    string ServiceId,
    int? DurationOverrideMinutes,
    int CapacityRequired);

public sealed record ResourceCapabilityResponse(
    string Id,
    string ResourceId,
    string ServiceId,
    int? DurationOverrideMinutes,
    int CapacityRequired,
    bool IsActive);

public sealed record AddServiceResourceRequirementRequest(
    string CategoryId,
    string RoleCode,
    int Quantity,
    bool IsOptional);

public sealed record ServiceResourceRequirementResponse(
    string Id,
    string ServiceId,
    string CategoryId,
    string RoleCode,
    int Quantity,
    bool IsOptional);
