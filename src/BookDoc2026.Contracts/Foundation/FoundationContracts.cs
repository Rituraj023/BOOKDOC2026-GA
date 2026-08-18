namespace BookDoc2026.Contracts.Foundation;

public sealed record SubmitTenantApplicationRequest(
    string LegalName,
    string Slug,
    string ContactEmail,
    string FirstBranchName,
    string FirstBranchCode);

public sealed record ApproveTenantApplicationRequest(long ExpectedVersion);

public sealed record TenantApplicationResponse(
    string Id,
    string LegalName,
    string Slug,
    string Status,
    long Version);

public sealed record TenantProvisioningResponse(
    string TenantId,
    string OrganizationId,
    string BranchId,
    string BranchConfigurationId,
    string OutboxMessageId);

public sealed record UpdateBranchConfigurationRequest(
    long ExpectedVersion,
    string? LogoUrl,
    string InvoicePrefix,
    string ReceiptPrefix,
    string? EmailSender,
    string? WhatsAppNumber);

public sealed record BranchConfigurationResponse(
    string BranchId,
    string BranchCode,
    string BranchName,
    string? LogoUrl,
    string InvoicePrefix,
    string ReceiptPrefix,
    string? EmailSender,
    string? WhatsAppNumber,
    string CommunicationVerificationStatus,
    long Version);

public sealed record CreateBranchAdministratorRequest(string SubjectId);

public sealed record BranchAdministratorResponse(
    string UserScopeId,
    string SubjectId,
    string BranchId,
    string DisplayName,
    string Email,
    string RoleCode,
    IReadOnlyCollection<string> Permissions);
