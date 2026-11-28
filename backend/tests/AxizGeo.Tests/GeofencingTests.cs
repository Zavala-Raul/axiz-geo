using AxizGeo.Application.DTOs;
using AxizGeo.Application.Geofencing;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace AxizGeo.Tests;

public class GeofencingTests
{
    private static readonly GeometryFactory Factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    [Fact]
    public void CircleGeofenceEvaluator_DetectsInsideAndOutsidePoints_InNuevoLaredo()
    {
        var resolver = new GeofenceEvaluatorResolver([new CircleGeofenceEvaluator(), new PolygonGeofenceEvaluator()]);
        var evaluator = resolver.Resolve(GeofenceType.Circle);

        // Aduana Puente III (-99.5371, 27.5973), radio 500 m
        var customsGeofence = new Geofence
        {
            Name = "Recinto Aduanal Puente III",
            Type = GeofenceType.Circle,
            Kind = GeofenceKind.Allowed,
            Geometry = Factory.CreatePoint(new Coordinate(-99.5371, 27.5973)),
            RadiusMeters = 500
        };

        var insidePoint = Factory.CreatePoint(new Coordinate(-99.5380, 27.5965)); // ~125 m
        var outsidePoint = Factory.CreatePoint(new Coordinate(-99.5500, 27.5850)); // ~1.8 km

        Assert.True(evaluator.IsInside(customsGeofence, insidePoint));
        Assert.False(evaluator.IsInside(customsGeofence, outsidePoint));
    }

    [Fact]
    public void PolygonGeofenceEvaluator_DetectsRestrictedZoneEntry()
    {
        var resolver = new GeofenceEvaluatorResolver([new CircleGeofenceEvaluator(), new PolygonGeofenceEvaluator()]);
        var evaluator = resolver.Resolve(GeofenceType.Polygon);

        var downtownPolygon = new Geofence
        {
            Name = "Zona Restringida Centro",
            Type = GeofenceType.Polygon,
            Kind = GeofenceKind.Restricted,
            Geometry = Factory.CreatePolygon(
            [
                new Coordinate(-99.5150, 27.5010),
                new Coordinate(-99.5010, 27.5010),
                new Coordinate(-99.5010, 27.4890),
                new Coordinate(-99.5150, 27.4890),
                new Coordinate(-99.5150, 27.5010)
            ])
        };

        var insideDowntown = Factory.CreatePoint(new Coordinate(-99.5080, 27.4950));
        var outsideInOradel = Factory.CreatePoint(new Coordinate(-99.5480, 27.4755));

        Assert.True(evaluator.IsInside(downtownPolygon, insideDowntown));
        Assert.False(evaluator.IsInside(downtownPolygon, outsideInOradel));
    }

    [Fact]
    public void CorridorEvaluator_DetectsOnRouteAndDeviation()
    {
        var corridorEvaluator = new CorridorEvaluator();
        var route = new GeoRoute
        {
            Name = "Puente III -> Oradel",
            CorridorMeters = 150,
            Path = Factory.CreateLineString(
            [
                new Coordinate(-99.5371, 27.5973),
                new Coordinate(-99.5590, 27.5550),
                new Coordinate(-99.5480, 27.4755)
            ])
        };

        var pointOnHighway = Factory.CreatePoint(new Coordinate(-99.5588, 27.5550)); // ~20 m del eje
        var pointDeviated450m = Factory.CreatePoint(new Coordinate(-99.5540, 27.5550)); // ~490 m al este

        Assert.True(corridorEvaluator.IsInsideCorridor(route, pointOnHighway));
        Assert.False(corridorEvaluator.IsInsideCorridor(route, pointDeviated450m));
    }

    [Fact]
    public void GeoJsonMapper_RejectsUnclosedPolygon()
    {
        var unclosed = new GeoJsonPolygonDto(
            "Polygon",
            [
                [
                    [-99.5150, 27.5010],
                    [-99.5010, 27.5010],
                    [-99.5010, 27.4890],
                    [-99.5150, 27.4890]
                ]
            ]);

        Assert.Throws<ArgumentException>(() => GeoJsonMapper.ToPolygon(unclosed));
    }
}
