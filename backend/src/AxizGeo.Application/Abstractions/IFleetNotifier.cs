using AxizGeo.Application.DTOs;

namespace AxizGeo.Application.Abstractions;

/// <summary>
/// Patrón Observer (puerto de salida): publica eventos de posiciones y alertas hacia canales suscritos (SignalR, Correo, etc.).
/// </summary>
public interface IFleetNotifier
{
    Task NotifyAlertRaisedAsync(AlertDto alert, CancellationToken ct = default);
    Task NotifyAlertUpdatedAsync(AlertDto alert, CancellationToken ct = default);
    Task NotifyPositionAcceptedAsync(Guid unitId, Guid tripId, CancellationToken ct = default);
}
