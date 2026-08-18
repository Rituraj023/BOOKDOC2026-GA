using BookDoc2026.Contracts.Security;

namespace BookDoc2026.Admin.Security;

public static class AdminPermissionPolicy
{
    private static readonly string[] CommunicationPermissions =
    [
        FoundationPermissions.MessageTemplatesView,
        FoundationPermissions.MessageTemplatesManage,
        FoundationPermissions.MessageTemplatesPublish,
        FoundationPermissions.MessageDeliveriesView,
        FoundationPermissions.CommunicationPreferencesView,
        FoundationPermissions.CommunicationPreferencesManage,
        FoundationPermissions.ProviderCallbacksView
    ];

    public static bool CanAccessCommunications(IEnumerable<string> permissions)
    {
        var granted = permissions.ToHashSet(StringComparer.Ordinal);
        return CommunicationPermissions.Any(granted.Contains);
    }
}
