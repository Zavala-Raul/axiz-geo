using AxizGeo.Application.DTOs;
using NetTopologySuite.Geometries;

namespace AxizGeo.Application.Abstractions;

public sealed record TruckDimensions(double? HeightMeters, double? WeightTons);

public sealed record CalculatedRouteResult(
    LineString Path,
    double DistanceMeters,
    double DurationSeconds);

/// <summary>
/// Patrón Adapter (puerto de salida): abstrae el cálculo externo de rutas para vehículo de carga pesada.
/// </summary>
public interface IRoutingProvider
{
    Task<CalculatedRouteResult> CalculateTruckRouteAsync(
        IReadOnlyList<GeoJsonPointDto> waypoints,
        TruckDimensions? truck = null,
        CancellationToken ct = default);
}
