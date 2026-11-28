using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AxizGeo.Api.Hubs;

[Authorize(Roles = "Admin,Monitor")]
public sealed class FleetHub : Hub
{
    public const string MonitorsGroup = "monitors";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, MonitorsGroup);
        await base.OnConnectedAsync();
    }
}
