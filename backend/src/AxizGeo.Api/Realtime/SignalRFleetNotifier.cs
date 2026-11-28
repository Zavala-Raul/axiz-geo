using AxizGeo.Api.Hubs;
using AxizGeo.Application.Abstractions;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace AxizGeo.Api.Realtime;

/// <summary>
/// Patrón Observer concreto: notifica en tiempo real al grupo 'monitors' vía SignalR y despacha canales externos registrados.
/// </summary>
public sealed class SignalRFleetNotifier(
    IHubContext<FleetHub> hubContext,
    IServiceProvider serviceProvider,
    IEnumerable<IAlertNotificationChannel> externalChannels,
    ILogger<SignalRFleetNotifier> logger) : IFleetNotifier
{
    public async Task NotifyAlertRaisedAsync(AlertDto alert, CancellationToken ct = default)
    {
        await hubContext.Clients.Group(FleetHub.MonitorsGroup)
            .SendAsync("AlertRaised", alert, ct);

        foreach (var channel in externalChannels)
        {
            try
            {
                await channel.SendAlertAsync(alert, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fallo en canal externo de notificación {Channel} para alerta {AlertId}",
                    channel.GetType().Name, alert.Id);
            }
        }
    }

    public Task NotifyAlertUpdatedAsync(AlertDto alert, CancellationToken ct = default) =>
        hubContext.Clients.Group(FleetHub.MonitorsGroup)
            .SendAsync("AlertUpdated", alert, ct);

    public async Task NotifyPositionAcceptedAsync(Guid unitId, Guid tripId, CancellationToken ct = default)
    {
        var fleetQuery = serviceProvider.GetRequiredService<FleetQueryService>();
        var liveUnit = await fleetQuery.GetLiveUnitAsync(unitId, ct);
        if (liveUnit is not null)
        {
            await hubContext.Clients.Group(FleetHub.MonitorsGroup)
                .SendAsync("UnitUpdated", liveUnit, ct);
        }
    }
}
