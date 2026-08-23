namespace BookDoc2026.Contracts.Security;

public static class FoundationPermissions
{
    public const string TenantsRegister = "Tenants.Register";
    public const string TenantsApprove = "Tenants.Approve";
    public const string BranchesView = "Branches.View";
    public const string BranchesConfigurationManage = "Branches.Configuration.Manage";
    public const string BranchAdministratorsManage = "Users.BranchAdministrators.Manage";
    public const string UsersManage = "Users.Manage";
    public const string PatientsRegister = "Patients.Register";
    public const string PatientsSearch = "Patients.Search";
    public const string PatientsView = "Patients.View";
    public const string PatientsUpdate = "Patients.Update";
    public const string CatalogView = "Catalog.View";
    public const string CatalogManage = "Catalog.Manage";
    public const string ResourcesView = "Resources.View";
    public const string ResourcesManage = "Resources.Manage";
    public const string ResourceStatusManage = "Resources.Status.Manage";
    public const string StakeholdersView = "Stakeholders.View";
    public const string StakeholdersManage = "Stakeholders.Manage";
    public const string StakeholderDocumentsManage = "Stakeholders.Documents.Manage";
    public const string SchedulingAvailabilityView = "Scheduling.Availability.View";
    public const string SchedulingAvailabilityManage = "Scheduling.Availability.Manage";
    public const string SchedulingHoldsCreate = "Scheduling.Holds.Create";
    public const string SchedulingHoldsRelease = "Scheduling.Holds.Release";
    public const string SchedulingBookingsConfirm = "Scheduling.Bookings.Confirm";
    public const string SchedulingBookingsView = "Scheduling.Bookings.View";
    public const string SchedulingBookingsCancel = "Scheduling.Bookings.Cancel";
    public const string SchedulingBookingsReschedule = "Scheduling.Bookings.Reschedule";
    public const string SchedulingWaitlistManage = "Scheduling.Waitlist.Manage";
    public const string SchedulingWaitlistView = "Scheduling.Waitlist.View";
    public const string ContractsView = "Contracts.View";
    public const string ContractsManage = "Contracts.Manage";
    public const string ContractEntitlementsReserve = "Contracts.Entitlements.Reserve";
    public const string ContractEntitlementsConsume = "Contracts.Entitlements.Consume";
    public const string ContractEntitlementsRelease = "Contracts.Entitlements.Release";
    public const string EncountersView = "Encounters.View";
    public const string EncounterDraftsManage = "Encounters.Drafts.Manage";
    public const string EncountersSign = "Encounters.Sign";
    public const string EncountersAmend = "Encounters.Amend";
    public const string PractitionersView = "Practitioners.View";
    public const string PractitionersManage = "Practitioners.Manage";
    public const string PractitionerCredentialsVerify = "Practitioners.Credentials.Verify";
    public const string PractitionerAssignmentsManage = "Practitioners.Assignments.Manage";
    public const string BillingInvoicesView = "Billing.Invoices.View";
    public const string BillingInvoicesIssue = "Billing.Invoices.Issue";
    public const string BillingPaymentsView = "Billing.Payments.View";
    public const string BillingPaymentsReceive = "Billing.Payments.Receive";
    public const string BillingPaymentsAllocate = "Billing.Payments.Allocate";
    public const string QueuesServicePointsManage = "Queues.ServicePoints.Manage";
    public const string QueuesView = "Queues.View";
    public const string QueuesCheckIn = "Queues.CheckIn";
    public const string QueuesCall = "Queues.Call";
    public const string QueuesProgress = "Queues.Progress";
    public const string QueuesCancel = "Queues.Cancel";
    public const string QueuesPriorityManage = "Queues.Priority.Manage";
    public const string QueuesDisplayView = "Queues.Display.View";
    public const string MessageDeliveriesView = "Messaging.Deliveries.View";
    public const string MessageTemplatesView = "Messaging.Templates.View";
    public const string MessageTemplatesManage = "Messaging.Templates.Manage";
    public const string MessageTemplatesPublish = "Messaging.Templates.Publish";
    public const string CommunicationPreferencesView = "Messaging.Preferences.View";
    public const string CommunicationPreferencesManage = "Messaging.Preferences.Manage";
    public const string ProviderCallbacksView = "Messaging.Callbacks.View";

    public static IReadOnlySet<string> BranchAssignable { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        BranchesView,
        BranchesConfigurationManage,
        PatientsRegister,
        PatientsSearch,
        PatientsView,
        PatientsUpdate,
        CatalogView,
        CatalogManage,
        ResourcesView,
        ResourcesManage,
        ResourceStatusManage,
        StakeholdersView,
        StakeholdersManage,
        StakeholderDocumentsManage,
        SchedulingAvailabilityView,
        SchedulingAvailabilityManage,
        SchedulingHoldsCreate,
        SchedulingHoldsRelease,
        SchedulingBookingsConfirm,
        SchedulingBookingsView,
        SchedulingBookingsCancel,
        SchedulingBookingsReschedule,
        SchedulingWaitlistManage,
        SchedulingWaitlistView,
        ContractsView,
        ContractsManage,
        ContractEntitlementsReserve,
        ContractEntitlementsConsume,
        ContractEntitlementsRelease,
        EncountersView,
        EncounterDraftsManage,
        EncountersSign,
        EncountersAmend,
        PractitionersView,
        PractitionersManage,
        PractitionerCredentialsVerify,
        PractitionerAssignmentsManage,
        BillingInvoicesView,
        BillingInvoicesIssue,
        BillingPaymentsView,
        BillingPaymentsReceive,
        BillingPaymentsAllocate,
        QueuesServicePointsManage,
        QueuesView,
        QueuesCheckIn,
        QueuesCall,
        QueuesProgress,
        QueuesCancel,
        QueuesPriorityManage,
        QueuesDisplayView,
        MessageDeliveriesView,
        MessageTemplatesView,
        MessageTemplatesManage,
        MessageTemplatesPublish,
        CommunicationPreferencesView,
        CommunicationPreferencesManage,
        ProviderCallbacksView
    };
}
