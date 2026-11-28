using AxizGeo.Domain.Enums;
using AxizGeo.Domain.States;
using NetTopologySuite.Geometries;

namespace AxizGeo.Domain.Entities;

public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.New;
    public string Message { get; set; } = string.Empty;
    public Point? Location { get; set; }

    public Guid? GeofenceId { get; set; }
    public Geofence? Geofence { get; set; }

    public bool IsHistorical { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? AttendedByUserId { get; set; }
    public string? AttendedByName { get; set; }
    public DateTime? AttendedAt { get; set; }
    public string? Resolution { get; set; }

    public void StartReview(Guid userId, string userName)
    {
        Status = AlertState.From(Status).StartReview().Status;
        AttendedByUserId = userId;
        AttendedByName = userName;
    }

    public void Resolve(Guid userId, string userName, string resolution, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(resolution) || resolution.Trim().Length < 5)
            throw new ArgumentException("La resolución debe contener al menos 5 caracteres.", nameof(resolution));

        Status = AlertState.From(Status).Resolve().Status;
        AttendedByUserId = userId;
        AttendedByName = userName;
        AttendedAt = utcNow;
        Resolution = resolution.Trim();
    }

    public void Dismiss(Guid userId, string userName, string resolution, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(resolution) || resolution.Trim().Length < 5)
            throw new ArgumentException("El motivo de descarte debe contener al menos 5 caracteres.", nameof(resolution));

        Status = AlertState.From(Status).Dismiss().Status;
        AttendedByUserId = userId;
        AttendedByName = userName;
        AttendedAt = utcNow;
        Resolution = resolution.Trim();
    }
}
