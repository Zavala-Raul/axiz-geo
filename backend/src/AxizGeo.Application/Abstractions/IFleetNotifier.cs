using AxizGeo.Application.DTOs;

namespace AxizGeo.Application.Abstractions;

/// <summary>
/// Canal enchufable de notificación externa para alertas (ej. Correo SMTP), desacoplado del motor de reglas.
/// </summary>
public interface IAlertNotificationChannel
{
    Task SendAlertAsync(AlertDto alert, CancellationToken ct = default);
}

/// <summary>
/// Patrón Observer (puerto de salida): publica eventos de posiciones y alertas hacia paneles suscritos y canales externos.
/// </summary>
public interface IFleetNotifier
{
    Task NotifyAlertRaisedAsync(AlertDto alert, CancellationToken ct = default);
    Task NotifyAlertUpdatedAsync(AlertDto alert, CancellationToken ct = default);
    Task NotifyPositionAcceptedAsync(Guid unitId, Guid tripId, CancellationToken ct = default);
}
