using AxizGeo.Domain.Enums;

namespace AxizGeo.Domain.Entities;

public sealed class InvalidTripTransitionException(TripStatus current, string action)
    : InvalidOperationException($"No se puede ejecutar '{action}' cuando el viaje está en estado '{current}'.");

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public Guid OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;

    public Guid RouteId { get; set; }
    public GeoRoute Route { get; set; } = null!;

    public TripStatus Status { get; set; } = TripStatus.Scheduled;
    public DateTime ScheduledStart { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? LastPositionAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Position> Positions { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];

    public void Start(DateTime utcNow)
    {
        if (Status != TripStatus.Scheduled)
            throw new InvalidTripTransitionException(Status, nameof(Start));

        Status = TripStatus.InProgress;
        StartedAt = utcNow;
    }

    public void Finish(DateTime utcNow)
    {
        if (Status != TripStatus.InProgress)
            throw new InvalidTripTransitionException(Status, nameof(Finish));

        Status = TripStatus.Completed;
        EndedAt = utcNow;
    }

    public void Cancel()
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new InvalidTripTransitionException(Status, nameof(Cancel));

        Status = TripStatus.Cancelled;
    }
}
