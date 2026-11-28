using NetTopologySuite.Geometries;

namespace AxizGeo.Application.Geofencing;

/// <summary>
/// Utilidades geodésicas sobre el elipsoide terrestre (WGS84 / SRID 4326) para cálculo de distancias en metros.
/// </summary>
public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_008.8;

    public static double HaversineMeters(Coordinate a, Coordinate b)
    {
        var dLat = DegreesToRadians(b.Y - a.Y);
        var dLon = DegreesToRadians(b.X - a.X);
        var lat1 = DegreesToRadians(a.Y);
        var lat2 = DegreesToRadians(b.Y);

        var sinHalfLat = Math.Sin(dLat / 2.0);
        var sinHalfLon = Math.Sin(dLon / 2.0);

        var h = sinHalfLat * sinHalfLat +
                Math.Cos(lat1) * Math.Cos(lat2) * sinHalfLon * sinHalfLon;

        return 2.0 * EarthRadiusMeters * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
    }

    public static double LineLengthMeters(LineString line)
    {
        var coords = line.Coordinates;
        var total = 0.0;
        for (var i = 0; i < coords.Length - 1; i++)
        {
            total += HaversineMeters(coords[i], coords[i + 1]);
        }
        return total;
    }

    public static double DistanceToLineMeters(LineString line, Point point)
    {
        var coords = line.Coordinates;
        if (coords.Length == 0)
            return double.PositiveInfinity;
        if (coords.Length == 1)
            return HaversineMeters(coords[0], point.Coordinate);

        var minMeters = double.PositiveInfinity;
        for (var i = 0; i < coords.Length - 1; i++)
        {
            var distance = DistanceToSegmentMeters(point.Coordinate, coords[i], coords[i + 1]);
            if (distance < minMeters)
                minMeters = distance;
        }

        return minMeters;
    }

    private static double DistanceToSegmentMeters(Coordinate p, Coordinate a, Coordinate b)
    {
        // Proyección equirectangular local centrada en el punto evaluado (alta exactitud para corredores en metros/km)
        var refLatRad = DegreesToRadians(p.Y);
        var cosLat = Math.Cos(refLatRad);

        var ax = DegreesToRadians(a.X - p.X) * EarthRadiusMeters * cosLat;
        var ay = DegreesToRadians(a.Y - p.Y) * EarthRadiusMeters;
        var bx = DegreesToRadians(b.X - p.X) * EarthRadiusMeters * cosLat;
        var by = DegreesToRadians(b.Y - p.Y) * EarthRadiusMeters;

        var dx = bx - ax;
        var dy = by - ay;
        var lenSq = dx * dx + dy * dy;

        if (lenSq <= 1e-9)
            return Math.Sqrt(ax * ax + ay * ay);

        // El punto p está en el origen (0,0), proyectamos sobre el segmento AB
        var t = Math.Clamp(-(ax * dx + ay * dy) / lenSq, 0.0, 1.0);
        var closestX = ax + t * dx;
        var closestY = ay + t * dy;

        return Math.Sqrt(closestX * closestX + closestY * closestY);
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
