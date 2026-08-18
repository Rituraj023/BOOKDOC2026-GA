using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Stakeholders;

public sealed class StakeholderDocumentReference : TenantScopedEntity
{
    private StakeholderDocumentReference()
    {
    }

    public long StakeholderId { get; private set; }

    public long DocumentTypeId { get; private set; }

    public long FileId { get; private set; }

    public string? ReferenceNumber { get; private set; }

    public DateOnly? IssuedOn { get; private set; }

    public DateOnly? ExpiresOn { get; private set; }

    public bool IsActive { get; private set; }

    public static StakeholderDocumentReference Create(
        long tenantId,
        long stakeholderId,
        long documentTypeId,
        long fileId,
        string? referenceNumber,
        DateOnly? issuedOn,
        DateOnly? expiresOn,
        DateTimeOffset now)
    {
        if (documentTypeId == 0 || fileId == 0)
        {
            throw new DomainRuleException("Document type and secure file identifiers are required.");
        }

        if (issuedOn.HasValue && expiresOn.HasValue && expiresOn < issuedOn)
        {
            throw new DomainRuleException("Document expiry cannot be before its issue date.");
        }

        var document = new StakeholderDocumentReference
        {
            TenantId = tenantId,
            StakeholderId = stakeholderId,
            DocumentTypeId = documentTypeId,
            FileId = fileId,
            ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber.Trim(),
            IssuedOn = issuedOn,
            ExpiresOn = expiresOn,
            IsActive = true
        };
        document.StampCreated(now);
        return document;
    }
}
