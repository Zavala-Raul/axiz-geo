using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;

namespace AxizGeo.Application.Alerts;

/// <summary>
/// Patrón Factory: centraliza la construcción consistente de alertas asignando severidad, mensaje en español y metadatos.
/// </summary>
public sealed class AlertFactory(TimeProvider timeProvider)
{
    public Alert CreateDeviationAlert(Trip trip, Position position, double distanceMeters)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return new Alert
        {
            TripId = trip.Id,
            Trip = trip,
            Type = AlertType.Deviation,
            Severity = AlertSeverity.High,
            Status = AlertStatus.New,
            Message = $"Unidad {trip.Unit.EconomicNumber} fuera del corredor autorizado ({Math.Round(distanceMeters)} m del eje en '{trip.Route.Name}').",
            Location = position.Location,
            IsHistorical = position.IsLate,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public Alert CreateRestrictedZoneAlert(Trip trip, Position position, Geofence geofence)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return new Alert
        {
            TripId = trip.Id,
            Trip = trip,
            Type = AlertType.RestrictedZone,
            Severity = AlertSeverity.High,
            Status = AlertStatus.New,
            Message = $"Unidad {trip.Unit.EconomicNumber} ingresó a la zona restringida '{geofence.Name}'.",
            Location = position.Location,
            GeofenceId = geofence.Id,
            Geofence = geofence,
            IsHistorical = position.IsLate,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public Alert CreateNoSignalAlert(Trip trip, int thresholdMinutes)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return new Alert
        {
            TripId = trip.Id,
            Trip = trip,
            Type = AlertType.NoSignal,
            Severity = AlertSeverity.Medium,
            Status = AlertStatus.New,
            Message = $"Unidad {trip.Unit.EconomicNumber} sin reportes de ubicación por más de {thresholdMinutes} minutos.",
            IsHistorical = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
