using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Queues;
using Microsoft.AspNetCore.SignalR;

namespace BookDoc2026.Api.Realtime;

public sealed class QueueRealtimeNotifier(
    IHubContext<QueueHub> hub,
    IPublicIdCodec publicIds) : IQueueRealtimeNotifier
{
    public async Task NotifyAsync(QueueChangedNotification notification, CancellationToken cancellationToken)
    {
        var message = new QueueRealtimeEvent(
            "QueueTicketChanged",
            publicIds.Encode(PublicIdKind.ImagingServicePoint, notification.ServicePointId, notification.TenantId),
            publicIds.Encode(PublicIdKind.QueueTicket, notification.TicketId, notification.TenantId),
            notification.Status.ToString(),
            notification.Version,
            notification.OccurredUtc);
        await hub.Clients.Group(BranchGroup(notification.BranchId))
            .SendAsync("QueueChanged", message, cancellationToken);
    }

    internal static string BranchGroup(string branchId) => $"queue:branch:{branchId}";
    internal static string BranchGroup(long branchId) => BranchGroup(branchId.ToString());
}
