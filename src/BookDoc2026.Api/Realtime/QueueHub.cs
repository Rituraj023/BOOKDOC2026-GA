using BookDoc2026.Contracts.Security;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookDoc2026.Api.Realtime;

[Authorize(Policy = FoundationPermissions.QueuesView)]
public sealed class QueueHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var branchIds = Context.User?.FindAll(BookDocClaimTypes.BranchId)
            .Select(claim => claim.Value)
            .Where(value => long.TryParse(value, out var parsed) && parsed > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
        foreach (var branchId in branchIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, QueueRealtimeNotifier.BranchGroup(branchId));
        await base.OnConnectedAsync();
    }
}
