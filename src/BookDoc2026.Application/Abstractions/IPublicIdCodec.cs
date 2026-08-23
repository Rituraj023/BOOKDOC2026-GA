using BookDoc2026.Domain.Common;

namespace BookDoc2026.Application.Abstractions;

public enum PublicIdKind
{
    TenantApplication,
    Tenant,
    Organization,
    Branch,
    BranchConfiguration,
    IdentitySubject,
    UserScope,
    OutboxMessage,
    Stakeholder,
    StakeholderContact,
    StakeholderIdentifier,
    StakeholderAddress,
    StakeholderDocument,
    DocumentType,
    StoredFile,
    Patient,
    ClinicalService,
    ResourceCategory,
    BookableResource,
    ResourceCapability,
    ServiceResourceRequirement,
    AvailabilityRule,
    AvailabilityException,
    SchedulingHold,
    Booking,
    BookingWaitlistEntry,
    Contract,
    ContractEntitlement,
    EntitlementReservation,
    Encounter,
    EncounterRevision,
    Practitioner,
    PractitionerCredential,
    PractitionerAssignment,
    Invoice,
    InvoiceLine,
    Payment,
    PaymentTender,
    PaymentAllocation,
    FinancialDocumentSnapshot,
    ImagingServicePoint,
    QueueTicket,
    MessageDeliveryAttempt,
    MessageTemplate,
    CommunicationPreferenceEvent,
    ProviderCallbackInbox,
    MessageDeliveryStatusEvent
}

public interface IPublicIdCodec
{
    string Encode(PublicIdKind kind, long id, long? tenantId = null);

    long Decode(PublicIdKind kind, string protectedId, long? expectedTenantId = null);
}

public static class PublicIdCodecExtensions
{
    public static long? DecodeOptional(this IPublicIdCodec codec, PublicIdKind kind, string? value, long? tenantId = null) =>
        string.IsNullOrWhiteSpace(value) ? null : codec.Decode(kind, value, tenantId);

    public static string? EncodeOptional(this IPublicIdCodec codec, PublicIdKind kind, long? value, long? tenantId = null) =>
        value.HasValue ? codec.Encode(kind, value.Value, tenantId) : null;
}
