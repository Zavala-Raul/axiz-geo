using NetTopologySuite.Geometries;

namespace AxizGeo.Domain.Entities;

public class GeoRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public MultiPoint Waypoints { get; set; } = null!;
    public LineString Path { get; set; } = null!;
    public int CorridorMeters { get; set; } = 150;
    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = "Sistema";

    public ICollection<Trip> Trips { get; set; } = [];
}
