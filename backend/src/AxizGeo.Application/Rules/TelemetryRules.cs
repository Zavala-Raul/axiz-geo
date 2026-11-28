using AxizGeo.Application.Alerts;
using AxizGeo.Application.Geofencing;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AxizGeo.Application.Rules;

public sealed class TelemetryEvaluationContext
{
    public required Trip Trip { get; init; }
    public required Position Position { get; init; }
    public Position? PreviousValidPosition { get; set; }
    public required IReadOnlyList<Geofence> ActiveRestrictedGeofences { get; init; }
    public required HashSet<AlertType> OpenAlertTypesForTrip { get; init; }
    public List<Alert> RaisedAlerts { get; } = [];
    public bool StopProcessing { get; set; }
    public bool IsPositionValidForTracking { get; set; } = true;
}

/// <summary>
/// Patrón Chain of Responsibility: cada regla evalúa de forma desacoplada una posición recibida y decide si continúa o corta la cadena.
/// </summary>
public interface ITelemetryRule
{
    int Order { get; }
    Task EvaluateAsync(TelemetryEvaluationContext context, CancellationToken ct = default);
}

public sealed class AccuracyRule(IOptions<TelemetryRulesOptions> options) : ITelemetryRule
{
    private readonly TelemetryRulesOptions _options = options.Value;
    public int Order => 10;

    public Task EvaluateAsync(TelemetryEvaluationContext context, CancellationToken ct = default)
    {
        if (context.Position.AccuracyMeters > _options.MaxAccuracyMeters)
        {
            context.IsPositionValidForTracking = false;
            context.StopProcessing = true;
        }

        return Task.CompletedTask;
    }
}

public sealed class DeviationRule(
    ICorridorEvaluator corridorEvaluator,
    AlertFactory alertFactory) : ITelemetryRule
{
    public int Order => 20;

    public Task EvaluateAsync(TelemetryEvaluationContext context, CancellationToken ct = default)
    {
        var distanceMeters = corridorEvaluator.DistanceToCorridorAxisMeters(context.Trip.Route, context.Position.Location);
        var insideCorridor = distanceMeters <= context.Trip.Route.CorridorMeters;
        context.Position.InsideCorridor = insideCorridor;

        if (insideCorridor)
            return Task.CompletedTask;

        var confirmedByConsecutive = context.PreviousValidPosition is { InsideCorridor: false };
        if (confirmedByConsecutive && !context.OpenAlertTypesForTrip.Contains(AlertType.Deviation))
        {
            var alert = alertFactory.CreateDeviationAlert(context.Trip, context.Position, distanceMeters);
            context.RaisedAlerts.Add(alert);
            context.OpenAlertTypesForTrip.Add(AlertType.Deviation);
        }

        return Task.CompletedTask;
    }
}

public sealed class RestrictedZoneRule(
    GeofenceEvaluatorResolver evaluatorResolver,
    AlertFactory alertFactory) : ITelemetryRule
{
    public int Order => 30;

    public Task EvaluateAsync(TelemetryEvaluationContext context, CancellationToken ct = default)
    {
        if (context.OpenAlertTypesForTrip.Contains(AlertType.RestrictedZone))
            return Task.CompletedTask;

        foreach (var geofence in context.ActiveRestrictedGeofences)
        {
            var evaluator = evaluatorResolver.Resolve(geofence.Type);
            if (evaluator.IsInside(geofence, context.Position.Location))
            {
                var alert = alertFactory.CreateRestrictedZoneAlert(context.Trip, context.Position, geofence);
                context.RaisedAlerts.Add(alert);
                context.OpenAlertTypesForTrip.Add(AlertType.RestrictedZone);
                break;
            }
        }

        return Task.CompletedTask;
    }
}

public sealed class TelemetryPipeline(IEnumerable<ITelemetryRule> rules)
{
    private readonly ITelemetryRule[] _orderedRules = rules.OrderBy(r => r.Order).ToArray();

    public async Task ExecuteAsync(TelemetryEvaluationContext context, CancellationToken ct = default)
    {
        foreach (var rule in _orderedRules)
        {
            if (context.StopProcessing)
                break;

            await rule.EvaluateAsync(context, ct);
        }
    }
}
