using AxizGeo.Application.Alerts;
using AxizGeo.Application.Geofencing;
using AxizGeo.Application.Rules;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace AxizGeo.Tests;

public class TelemetryPipelineTests
{
    private static readonly GeometryFactory Factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private static TelemetryPipeline CreatePipeline()
    {
        var options = Options.Create(new TelemetryRulesOptions
        {
            MaxAccuracyMeters = 50,
            DefaultCorridorMeters = 150,
            ConsecutiveDeviationsRequired = 2
        });

        var alertFactory = new AlertFactory(TimeProvider.System);
        var corridorEvaluator = new CorridorEvaluator();
        var geofenceResolver = new GeofenceEvaluatorResolver([
            new CircleGeofenceEvaluator(),
            new PolygonGeofenceEvaluator()
        ]);

        return new TelemetryPipeline([
            new AccuracyRule(options),
            new DeviationRule(corridorEvaluator, alertFactory),
            new RestrictedZoneRule(geofenceResolver, alertFactory)
        ]);
    }

    private static Trip CreateSampleTrip()
    {
        var unit = new Unit { EconomicNumber = "T-101", Plates = "84-AK-2L", Brand = "Kenworth", Model = "T680", Year = 2024 };
        var op = new Operator { FullName = "Operador Prueba", LicenseNumber = "FED-001", Phone = "8670000000", Email = "op@axiz.demo" };
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

        return new Trip
        {
            UnitId = unit.Id,
            Unit = unit,
            OperatorId = op.Id,
            Operator = op,
            RouteId = route.Id,
            Route = route,
            Status = TripStatus.InProgress
        };
    }

    [Fact]
    public async Task AccuracyRule_StopsChain_WhenAccuracyExceedsThreshold()
    {
        var pipeline = CreatePipeline();
        var trip = CreateSampleTrip();
        var noisyPosition = new Domain.Entities.Position
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            Location = Factory.CreatePoint(new Coordinate(-99.5100, 27.4950)), // muy lejos de la ruta
            AccuracyMeters = 120.0, // > 50m
            CapturedAt = DateTime.UtcNow
        };

        var ctx = new TelemetryEvaluationContext
        {
            Trip = trip,
            Position = noisyPosition,
            ActiveRestrictedGeofences = [],
            OpenAlertTypesForTrip = []
        };

        await pipeline.ExecuteAsync(ctx);

        Assert.True(ctx.StopProcessing);
        Assert.False(ctx.IsPositionValidForTracking);
        Assert.Empty(ctx.RaisedAlerts);
        Assert.Null(noisyPosition.InsideCorridor);
    }

    [Fact]
    public async Task DeviationRule_RequiresTwoConsecutiveOutsidePositions_AndPreventsDuplicates()
    {
        var pipeline = CreatePipeline();
        var trip = CreateSampleTrip();
        var openAlerts = new HashSet<AlertType>();

        // 1ra posición fuera del corredor (~490m al este): NO debe levantar alerta todavía
        var pos1 = new Domain.Entities.Position
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            Location = Factory.CreatePoint(new Coordinate(-99.5540, 27.5550)),
            AccuracyMeters = 10,
            CapturedAt = DateTime.UtcNow.AddSeconds(-30)
        };

        var ctx1 = new TelemetryEvaluationContext
        {
            Trip = trip,
            Position = pos1,
            PreviousValidPosition = null,
            ActiveRestrictedGeofences = [],
            OpenAlertTypesForTrip = openAlerts
        };
        await pipeline.ExecuteAsync(ctx1);

        Assert.Equal(false, pos1.InsideCorridor);
        Assert.Empty(ctx1.RaisedAlerts);

        // 2da posición consecutiva fuera del corredor: SÍ confirma la alerta de desviación
        var pos2 = new Domain.Entities.Position
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            Location = Factory.CreatePoint(new Coordinate(-99.5535, 27.5540)),
            AccuracyMeters = 10,
            CapturedAt = DateTime.UtcNow
        };

        var ctx2 = new TelemetryEvaluationContext
        {
            Trip = trip,
            Position = pos2,
            PreviousValidPosition = pos1,
            ActiveRestrictedGeofences = [],
            OpenAlertTypesForTrip = openAlerts
        };
        await pipeline.ExecuteAsync(ctx2);

        Assert.Single(ctx2.RaisedAlerts);
        Assert.Equal(AlertType.Deviation, ctx2.RaisedAlerts[0].Type);

        // 3ra posición fuera: como ya existe alerta abierta en openAlerts, NO debe duplicarla (anti-spam)
        var pos3 = new Domain.Entities.Position
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            Location = Factory.CreatePoint(new Coordinate(-99.5530, 27.5530)),
            AccuracyMeters = 10,
            CapturedAt = DateTime.UtcNow.AddSeconds(30)
        };

        var ctx3 = new TelemetryEvaluationContext
        {
            Trip = trip,
            Position = pos3,
            PreviousValidPosition = pos2,
            ActiveRestrictedGeofences = [],
            OpenAlertTypesForTrip = openAlerts
        };
        await pipeline.ExecuteAsync(ctx3);

        Assert.Empty(ctx3.RaisedAlerts);
    }

    [Fact]
    public async Task RestrictedZoneRule_RaisesHighSeverityAlert_OnZoneEntry()
    {
        var pipeline = CreatePipeline();
        var trip = CreateSampleTrip();

        var restrictedPolygon = new Geofence
        {
            Name = "Centro Histórico Prohibido",
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

        var posInRestricted = new Domain.Entities.Position
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            Location = Factory.CreatePoint(new Coordinate(-99.5080, 27.4950)),
            AccuracyMeters = 8,
            CapturedAt = DateTime.UtcNow
        };

        var ctx = new TelemetryEvaluationContext
        {
            Trip = trip,
            Position = posInRestricted,
            PreviousValidPosition = null,
            ActiveRestrictedGeofences = [restrictedPolygon],
            OpenAlertTypesForTrip = []
        };

        await pipeline.ExecuteAsync(ctx);

        Assert.Single(ctx.RaisedAlerts);
        Assert.Equal(AlertType.RestrictedZone, ctx.RaisedAlerts[0].Type);
        Assert.Equal(AlertSeverity.High, ctx.RaisedAlerts[0].Severity);
        Assert.Equal(restrictedPolygon.Id, ctx.RaisedAlerts[0].GeofenceId);
    }
}
