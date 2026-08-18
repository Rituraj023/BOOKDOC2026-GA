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
        MessageDeliveriesView,
        MessageTemplatesView,
        MessageTemplatesManage,
        MessageTemplatesPublish,
        CommunicationPreferencesView,
        CommunicationPreferencesManage,
        ProviderCallbacksView
    };
}
