using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using NetTopologySuite.Geometries;

namespace AxizGeo.Application.Geofencing;

/// <summary>
/// Patrón Strategy: define una familia de algoritmos intercambiables para evaluar si una coordenada cae dentro de una geocerca.
/// </summary>
public interface IGeofenceEvaluator
{
    GeofenceType Type { get; }
    bool IsInside(Geofence geofence, Point point);
}

public sealed class CircleGeofenceEvaluator : IGeofenceEvaluator
{
    public GeofenceType Type => GeofenceType.Circle;

    public bool IsInside(Geofence geofence, Point point)
    {
        if (geofence.Geometry is not Point center || geofence.RadiusMeters is null or <= 0)
            return false;

        return GeoMath.HaversineMeters(center.Coordinate, point.Coordinate) <= geofence.RadiusMeters.Value;
    }
}

public sealed class PolygonGeofenceEvaluator : IGeofenceEvaluator
{
    public GeofenceType Type => GeofenceType.Polygon;

    public bool IsInside(Geofence geofence, Point point)
    {
        if (geofence.Geometry is not Polygon polygon)
            return false;

        return polygon.Covers(point);
    }
}

public sealed class GeofenceEvaluatorResolver(IEnumerable<IGeofenceEvaluator> evaluators)
{
    private readonly Dictionary<GeofenceType, IGeofenceEvaluator> _strategies =
        evaluators.ToDictionary(e => e.Type);

    public IGeofenceEvaluator Resolve(GeofenceType type) =>
        _strategies.TryGetValue(type, out var evaluator)
            ? evaluator
            : throw new InvalidOperationException($"No existe estrategia registrada para el tipo de geocerca '{type}'.");
}

public interface ICorridorEvaluator
{
    bool IsInsideCorridor(GeoRoute route, Point point);
    double DistanceToCorridorAxisMeters(GeoRoute route, Point point);
}

public sealed class CorridorEvaluator : ICorridorEvaluator
{
    public bool IsInsideCorridor(GeoRoute route, Point point) =>
        DistanceToCorridorAxisMeters(route, point) <= route.CorridorMeters;

    public double DistanceToCorridorAxisMeters(GeoRoute route, Point point) =>
        GeoMath.DistanceToLineMeters(route.Path, point);
}
