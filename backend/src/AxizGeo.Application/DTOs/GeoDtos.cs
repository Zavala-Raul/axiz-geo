using System.Text.Json;
using AxizGeo.Domain.Enums;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace AxizGeo.Application.DTOs;

public sealed record GeoJsonPointDto(string Type, double[] Coordinates)
{
    public static GeoJsonPointDto FromPoint(Point point) =>
        new("Point", [point.X, point.Y]);
}

public sealed record GeoJsonLineStringDto(string Type, double[][] Coordinates)
{
    public static GeoJsonLineStringDto FromLineString(LineString line) =>
        new("LineString", line.Coordinates.Select(c => new[] { c.X, c.Y }).ToArray());
}

public sealed record GeoJsonPolygonDto(string Type, double[][][] Coordinates)
{
    public static GeoJsonPolygonDto FromPolygon(Polygon polygon)
    {
        var rings = new List<double[][]>
        {
            polygon.ExteriorRing.Coordinates.Select(c => new[] { c.X, c.Y }).ToArray()
        };

        for (var i = 0; i < polygon.NumInteriorRings; i++)
        {
            rings.Add(polygon.GetInteriorRingN(i).Coordinates.Select(c => new[] { c.X, c.Y }).ToArray());
        }

        return new GeoJsonPolygonDto("Polygon", rings.ToArray());
    }
}

public static class GeoJsonMapper
{
    private static readonly GeometryFactory Factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Point ToPoint(GeoJsonPointDto dto)
    {
        if (!string.Equals(dto.Type, "Point", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El tipo GeoJSON debe ser 'Point'.");

        ValidateLonLat(dto.Coordinates);
        return Factory.CreatePoint(new Coordinate(dto.Coordinates[0], dto.Coordinates[1]));
    }

    public static MultiPoint ToMultiPoint(IReadOnlyList<GeoJsonPointDto> points)
    {
        if (points is null || points.Count < 2 || points.Count > 50)
            throw new ArgumentException("Se requieren entre 2 y 50 waypoints.");

        var coords = points.Select(p =>
        {
            if (!string.Equals(p.Type, "Point", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Cada waypoint debe tener tipo GeoJSON 'Point'.");
            ValidateLonLat(p.Coordinates);
            return new Coordinate(p.Coordinates[0], p.Coordinates[1]);
        }).ToArray();

        return Factory.CreateMultiPointFromCoords(coords);
    }

    public static IReadOnlyList<GeoJsonPointDto> FromMultiPoint(MultiPoint multiPoint) =>
        multiPoint.Coordinates.Select(c => new GeoJsonPointDto("Point", [c.X, c.Y])).ToList();

    public static LineString ToLineString(GeoJsonLineStringDto dto)
    {
        if (!string.Equals(dto.Type, "LineString", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El tipo GeoJSON debe ser 'LineString'.");

        if (dto.Coordinates is null || dto.Coordinates.Length < 2)
            throw new ArgumentException("La ruta ('LineString') debe contener al menos 2 coordenadas.");

        var coords = dto.Coordinates.Select(c =>
        {
            ValidateLonLat(c);
            return new Coordinate(c[0], c[1]);
        }).ToArray();

        return Factory.CreateLineString(coords);
    }

    public static Polygon ToPolygon(GeoJsonPolygonDto dto)
    {
        if (!string.Equals(dto.Type, "Polygon", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El tipo GeoJSON debe ser 'Polygon'.");

        if (dto.Coordinates is null || dto.Coordinates.Length == 0)
            throw new ArgumentException("El polígono debe contener al menos un anillo exterior.");

        var outerRaw = dto.Coordinates[0];
        if (outerRaw is null || outerRaw.Length < 4 || outerRaw.Length > 201)
            throw new ArgumentException("El anillo del polígono debe estar cerrado y tener entre 3 y 200 vértices.");

        var outerCoords = outerRaw.Select(c =>
        {
            ValidateLonLat(c);
            return new Coordinate(c[0], c[1]);
        }).ToArray();

        if (!outerCoords[0].Equals2D(outerCoords[^1]))
            throw new ArgumentException("El polígono debe estar cerrado (el primer punto debe coincidir con el último).");

        var shell = Factory.CreateLinearRing(outerCoords);
        var polygon = Factory.CreatePolygon(shell);

        if (!polygon.IsValid)
            throw new ArgumentException("La geometría del polígono no es válida (posible auto-intersección).");

        return polygon;
    }

    public static Geometry ParseGeofenceGeometry(GeofenceType geofenceType, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("El campo 'geometry' debe ser un objeto GeoJSON válido.");

        if (geofenceType == GeofenceType.Circle)
        {
            var pointDto = element.Deserialize<GeoJsonPointDto>(JsonOptions)
                ?? throw new ArgumentException("Geometría 'Point' inválida para geocerca circular.");
            return ToPoint(pointDto);
        }

        var polygonDto = element.Deserialize<GeoJsonPolygonDto>(JsonOptions)
            ?? throw new ArgumentException("Geometría 'Polygon' inválida para geocerca poligonal.");
        return ToPolygon(polygonDto);
    }

    public static JsonElement ToJsonElement(Geometry geometry) => geometry switch
    {
        Point point => JsonSerializer.SerializeToElement(GeoJsonPointDto.FromPoint(point), JsonOptions),
        Polygon polygon => JsonSerializer.SerializeToElement(GeoJsonPolygonDto.FromPolygon(polygon), JsonOptions),
        _ => throw new InvalidOperationException($"Tipo de geometría '{geometry.GeometryType}' no soportado en geocercas.")
    };

    private static void ValidateLonLat(double[]? coord)
    {
        if (coord is null || coord.Length < 2 || double.IsNaN(coord[0]) || double.IsNaN(coord[1]))
            throw new ArgumentException("Cada coordenada debe contener [longitud, latitud].");

        var lng = coord[0];
        var lat = coord[1];

        if (lng < -180 || lng > 180)
            throw new ArgumentException($"Longitud fuera de rango [-180, 180]: {lng}. Verifica el orden [longitud, latitud].");

        if (lat < -90 || lat > 90)
            throw new ArgumentException($"Latitud fuera de rango [-90, 90]: {lat}. Verifica el orden [longitud, latitud].");
    }
}
