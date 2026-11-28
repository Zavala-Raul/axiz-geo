using AxizGeo.Domain.Enums;
using NetTopologySuite.Geometries;

namespace AxizGeo.Domain.Entities;

public class Geofence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public GeofenceType Type { get; set; }
    public GeofenceKind Kind { get; set; }
    public Geometry Geometry { get; set; } = null!;
    public double? RadiusMeters { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
