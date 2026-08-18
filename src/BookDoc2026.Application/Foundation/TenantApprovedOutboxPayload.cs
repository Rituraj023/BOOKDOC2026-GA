namespace BookDoc2026.Application.Foundation;

public sealed record TenantApprovedOutboxPayload(
    long TenantApplicationId,
    long TenantId,
    long OrganizationId,
    long BranchId);
