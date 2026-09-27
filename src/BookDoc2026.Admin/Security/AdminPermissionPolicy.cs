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

    public static bool CanManageQueueServicePoints(IEnumerable<string> permissions)
        => permissions.Contains(FoundationPermissions.QueuesServicePointsManage, StringComparer.Ordinal);

    public static bool CanAccessBillingOversight(IEnumerable<string> permissions)
    {
        var granted = permissions.ToHashSet(StringComparer.Ordinal);
        return granted.Contains(FoundationPermissions.BillingInvoicesView)
            || granted.Contains(FoundationPermissions.BillingPaymentsView);
    }

    public static bool CanManageDoctorSlots(IEnumerable<string> permissions)
    {
        var granted = permissions.ToHashSet(StringComparer.Ordinal);
        return granted.Contains(FoundationPermissions.SchedulingAvailabilityManage)
            || granted.Contains(FoundationPermissions.SchedulingAvailabilityView);
    }

    public static bool CanReviewBookingRequests(IEnumerable<string> permissions)
    {
        var granted = permissions.ToHashSet(StringComparer.Ordinal);
        return granted.Contains(FoundationPermissions.SchedulingBookingsView)
            || granted.Contains(FoundationPermissions.SchedulingBookingsConfirm);
    }
}
