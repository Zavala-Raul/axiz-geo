using AxizGeo.Domain.Enums;
using NetTopologySuite.Geometries;

namespace AxizGeo.Domain.Entities;

public class Position
{
    /// <summary>
    /// Identificador UUID generado por el cliente para garantizar idempotencia en reenvíos tras desconexión.
    /// </summary>
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public Point Location { get; set; } = null!;
    public double AccuracyMeters { get; set; }
    public double? SpeedKmh { get; set; }
    public double? Heading { get; set; }
    public DateTime CapturedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public PositionSource Source { get; set; } = PositionSource.Simulator;
    public bool? InsideCorridor { get; set; }
    public bool IsLate { get; set; }
}
