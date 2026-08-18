using System.Text;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Scheduling;
using BookDoc2026.Domain.Stakeholders;
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
