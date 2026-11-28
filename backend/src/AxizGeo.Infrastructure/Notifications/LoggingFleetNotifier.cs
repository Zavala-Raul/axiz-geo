using AxizGeo.Application.Abstractions;
using AxizGeo.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace AxizGeo.Infrastructure.Notifications;

public sealed class LoggingFleetNotifier(ILogger<LoggingFleetNotifier> logger) : IFleetNotifier
{
    public Task NotifyAlertRaisedAsync(AlertDto alert, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Alerta generada [{Type}/{Severity}] para unidad {Unit}: {Message}",
            alert.Type,
            alert.Severity,
            alert.Unit.EconomicNumber,
            alert.Message);
        return Task.CompletedTask;
    }

    public Task NotifyAlertUpdatedAsync(AlertDto alert, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Alerta actualizada [{Id}] -> {Status}",
            alert.Id,
            alert.Status);
        return Task.CompletedTask;
    }

    public Task NotifyPositionAcceptedAsync(Guid unitId, Guid tripId, CancellationToken ct = default)
    {
        logger.LogDebug("Posición procesada para unidad {UnitId} en viaje {TripId}", unitId, tripId);
        return Task.CompletedTask;
    }
}
