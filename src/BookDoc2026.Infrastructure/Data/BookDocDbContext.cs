using System.Text;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Billing;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Contracts;
using BookDoc2026.Domain.Clinical;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Radiology;
using BookDoc2026.Domain.Scheduling;
using BookDoc2026.Domain.Stakeholders;
using BookDoc2026.Domain.Workforce;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BookDoc2026.Infrastructure.Data;

public sealed class BookDocDbContext(
    DbContextOptions<BookDocDbContext> options,
    ICurrentActor currentActor)
    : IdentityDbContext<ApplicationUser, ApplicationRole, uint,
        ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin,
        ApplicationRoleClaim, ApplicationUserToken>(options)
{
    public DbSet<ApplicationUserScope> UserScopes => Set<ApplicationUserScope>();

    public DbSet<TenantApplication> TenantApplications => Set<TenantApplication>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<BranchConfiguration> BranchConfigurations => Set<BranchConfiguration>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();

    public DbSet<MessageDeliveryAttempt> MessageDeliveryAttempts => Set<MessageDeliveryAttempt>();

    public DbSet<CommunicationPreferenceEvent> CommunicationPreferenceEvents => Set<CommunicationPreferenceEvent>();

    public DbSet<ProviderCallbackInbox> ProviderCallbackInboxes => Set<ProviderCallbackInbox>();

    public DbSet<MessageDeliveryStatusEvent> MessageDeliveryStatusEvents => Set<MessageDeliveryStatusEvent>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Stakeholder> Stakeholders => Set<Stakeholder>();

    public DbSet<StakeholderPerson> StakeholderPersons => Set<StakeholderPerson>();

    public DbSet<StakeholderCorporate> StakeholderCorporates => Set<StakeholderCorporate>();

    public DbSet<StakeholderContactPoint> StakeholderContactPoints => Set<StakeholderContactPoint>();

    public DbSet<StakeholderIdentifier> StakeholderIdentifiers => Set<StakeholderIdentifier>();

    public DbSet<StakeholderAddress> StakeholderAddresses => Set<StakeholderAddress>();

    public DbSet<StakeholderDocumentReference> StakeholderDocumentReferences => Set<StakeholderDocumentReference>();

    public DbSet<ClinicalService> ClinicalServices => Set<ClinicalService>();

    public DbSet<ResourceCategory> ResourceCategories => Set<ResourceCategory>();

    public DbSet<BookableResource> BookableResources => Set<BookableResource>();

    public DbSet<ResourceCapability> ResourceCapabilities => Set<ResourceCapability>();

    public DbSet<ServiceResourceRequirement> ServiceResourceRequirements => Set<ServiceResourceRequirement>();

    public DbSet<ResourceStatusEvent> ResourceStatusEvents => Set<ResourceStatusEvent>();
    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();
    public DbSet<AvailabilityException> AvailabilityExceptions => Set<AvailabilityException>();
    public DbSet<SchedulingHold> SchedulingHolds => Set<SchedulingHold>();
    public DbSet<ResourceReservation> ResourceReservations => Set<ResourceReservation>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingResourceAllocation> BookingResourceAllocations => Set<BookingResourceAllocation>();
    public DbSet<BookingWaitlistEntry> BookingWaitlistEntries => Set<BookingWaitlistEntry>();
    public DbSet<ImagingServicePoint> ImagingServicePoints => Set<ImagingServicePoint>();
    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();
    public DbSet<QueueTicketEvent> QueueTicketEvents => Set<QueueTicketEvent>();
    public DbSet<ContractAgreement> Contracts => Set<ContractAgreement>();
    public DbSet<ContractEntitlement> ContractEntitlements => Set<ContractEntitlement>();
    public DbSet<EntitlementReservation> EntitlementReservations => Set<EntitlementReservation>();
    public DbSet<ClinicalEncounter> ClinicalEncounters => Set<ClinicalEncounter>();
    public DbSet<EncounterRevision> EncounterRevisions => Set<EncounterRevision>();
    public DbSet<InvestigationOrder> InvestigationOrders => Set<InvestigationOrder>();
    public DbSet<InvestigationOrderEvent> InvestigationOrderEvents => Set<InvestigationOrderEvent>();
    public DbSet<RadiologyStudy> RadiologyStudies => Set<RadiologyStudy>();
    public DbSet<RadiologyStudyEvent> RadiologyStudyEvents => Set<RadiologyStudyEvent>();
    public DbSet<RadiologyAcquisitionAttempt> RadiologyAcquisitionAttempts => Set<RadiologyAcquisitionAttempt>();
    public DbSet<RadiologyQualityReview> RadiologyQualityReviews => Set<RadiologyQualityReview>();
    public DbSet<PhysiotherapyCarePlan> PhysiotherapyCarePlans => Set<PhysiotherapyCarePlan>();
    public DbSet<PhysiotherapyCarePlanRevision> PhysiotherapyCarePlanRevisions => Set<PhysiotherapyCarePlanRevision>();
    public DbSet<PhysiotherapyTreatmentSession> PhysiotherapyTreatmentSessions => Set<PhysiotherapyTreatmentSession>();
    public DbSet<PhysiotherapyOutcomeObservation> PhysiotherapyOutcomeObservations => Set<PhysiotherapyOutcomeObservation>();
    public DbSet<PractitionerProfile> PractitionerProfiles => Set<PractitionerProfile>();
    public DbSet<PractitionerCredential> PractitionerCredentials => Set<PractitionerCredential>();
    public DbSet<PractitionerAssignment> PractitionerAssignments => Set<PractitionerAssignment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentTender> PaymentTenders => Set<PaymentTender>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<FinancialDocumentSnapshot> FinancialDocumentSnapshots => Set<FinancialDocumentSnapshot>();

    public long? CurrentTenantId => currentActor.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookDocDbContext).Assembly);

        modelBuilder.Entity<Organization>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<Branch>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<BranchConfiguration>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<Patient>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<Stakeholder>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<StakeholderPerson>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<StakeholderCorporate>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<StakeholderContactPoint>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<StakeholderIdentifier>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<StakeholderAddress>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<StakeholderDocumentReference>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ClinicalService>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ResourceCategory>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<BookableResource>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ResourceCapability>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ServiceResourceRequirement>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ResourceStatusEvent>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<AvailabilityRule>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<AvailabilityException>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<SchedulingHold>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ResourceReservation>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<Booking>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<BookingResourceAllocation>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<BookingWaitlistEntry>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ImagingServicePoint>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<QueueTicket>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<QueueTicketEvent>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ContractAgreement>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ContractEntitlement>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<EntitlementReservation>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ClinicalEncounter>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<EncounterRevision>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<InvestigationOrder>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<InvestigationOrderEvent>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<RadiologyStudy>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<RadiologyStudyEvent>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<RadiologyAcquisitionAttempt>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<RadiologyQualityReview>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PhysiotherapyCarePlan>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PhysiotherapyCarePlanRevision>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PhysiotherapyTreatmentSession>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PhysiotherapyOutcomeObservation>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PractitionerProfile>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PractitionerCredential>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PractitionerAssignment>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<Invoice>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<InvoiceLine>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<Payment>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PaymentTender>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<PaymentAllocation>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<FinancialDocumentSnapshot>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<MessageTemplate>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<MessageDeliveryAttempt>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<CommunicationPreferenceEvent>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<ProviderCallbackInbox>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);
        modelBuilder.Entity<MessageDeliveryStatusEvent>()
            .HasQueryFilter(entity => CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId.Value);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                entityType.FindProperty(nameof(Entity.Id))!.ValueGenerated = ValueGenerated.Never;
            }

            ApplyPhysicalNaming(entityType);
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    private static void ApplyPhysicalNaming(IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0
                && (char.IsLower(value[index - 1])
                    || (index + 1 < value.Length && char.IsLower(value[index + 1]))))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ProtectAuditEvents();
        ProtectResourceStatusEvents();
        ProtectMessageDeliveryAttempts();
        ProtectCommunicationPreferenceEvents();
        ProtectMessageDeliveryStatusEvents();
        ProtectBookingResourceAllocations();
        ProtectQueueTicketEvents();
        ProtectEncounterRevisions();
        ProtectInvestigationOrderEvents();
        ProtectRadiologyHistory();
        ProtectPhysiotherapyClinicalHistory();
        ProtectPostedFinancialRecords();
        ProtectTenantScope();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ProtectAuditEvents()
    {
        if (ChangeTracker.Entries<AuditEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Audit events are append-only.");
        }
    }

    private void ProtectResourceStatusEvents()
    {
        if (ChangeTracker.Entries<ResourceStatusEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Resource status events are append-only.");
        }
    }

    private void ProtectMessageDeliveryAttempts()
    {
        if (ChangeTracker.Entries<MessageDeliveryAttempt>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Message delivery attempts are append-only.");
        }
    }

    private void ProtectCommunicationPreferenceEvents()
    {
        if (ChangeTracker.Entries<CommunicationPreferenceEvent>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Communication preference events are append-only.");
    }

    private void ProtectMessageDeliveryStatusEvents()
    {
        if (ChangeTracker.Entries<MessageDeliveryStatusEvent>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Message delivery status events are append-only.");
    }

    private void ProtectBookingResourceAllocations()
    {
        if (ChangeTracker.Entries<BookingResourceAllocation>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Booking resource allocations are immutable.");
    }

    private void ProtectQueueTicketEvents()
    {
        if (ChangeTracker.Entries<QueueTicketEvent>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Queue ticket events are append-only.");
    }

    private void ProtectEncounterRevisions()
    {
        if (ChangeTracker.Entries<EncounterRevision>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Clinical encounter revisions are append-only.");
    }

    private void ProtectInvestigationOrderEvents()
    {
        if (ChangeTracker.Entries<InvestigationOrderEvent>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Investigation order events are append-only.");
    }

    private void ProtectRadiologyHistory()
    {
        if (ChangeTracker.Entries<RadiologyStudyEvent>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<RadiologyAcquisitionAttempt>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<RadiologyQualityReview>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException(
                "Radiology study events, acquisition attempts and quality reviews are append-only.");
    }

    private void ProtectPhysiotherapyClinicalHistory()
    {
        if (ChangeTracker.Entries<PhysiotherapyCarePlanRevision>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<PhysiotherapyTreatmentSession>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<PhysiotherapyOutcomeObservation>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Physiotherapy revisions, sessions and outcomes are append-only.");
    }

    private void ProtectPostedFinancialRecords()
    {
        if (ChangeTracker.Entries<Invoice>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<Payment>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Posted invoices and payments cannot be deleted.");
        if (ChangeTracker.Entries<InvoiceLine>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<PaymentTender>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<PaymentAllocation>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<FinancialDocumentSnapshot>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Posted financial details and snapshots are immutable.");
    }

    private void ProtectTenantScope()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            if (currentActor.IsPlatformOperator)
            {
                continue;
            }

            if (currentActor.TenantId is null || entry.Entity.TenantId != currentActor.TenantId.Value)
            {
                throw new ForbiddenException("A tenant-scoped entity cannot be written outside the actor tenant.");
            }

            if (entry.State == EntityState.Modified)
            {
                PropertyEntry tenantProperty = entry.Property(nameof(ITenantScoped.TenantId));
                if (tenantProperty.IsModified)
                {
                    throw new ForbiddenException("Tenant ownership cannot be changed.");
                }
            }
        }
    }
}
