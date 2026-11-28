using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Rules;
using AxizGeo.Domain.Enums;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Simplify;

namespace AxizGeo.Application.Services;

public sealed class FleetQueryService(
    IUnitOfWork uow,
    IFleetReadRepository fleetReadRepository,
    TimeProvider timeProvider,
    IOptions<TelemetryRulesOptions> rulesOptions)
{
    private readonly TelemetryRulesOptions _options = rulesOptions.Value;
    private static readonly GeometryFactory GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public async Task<IReadOnlyList<LiveUnitDto>> GetLiveFleetAsync(CancellationToken ct = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var snapshots = await fleetReadRepository.GetFleetSnapshotsAsync(null, ct);

        return snapshots
            .Select(s => MapSnapshotToDto(s, nowUtc, _options.NoSignalMinutes))
            .ToList();
    }

    public async Task<LiveUnitDto?> GetLiveUnitAsync(Guid unitId, CancellationToken ct = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var snapshots = await fleetReadRepository.GetFleetSnapshotsAsync(unitId, ct);
        var snapshot = snapshots.FirstOrDefault();

        return snapshot is null
            ? null
            : MapSnapshotToDto(snapshot, nowUtc, _options.NoSignalMinutes);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        var liveUnits = await GetLiveFleetAsync(ct);
        var totalOpenAlerts = await fleetReadRepository.CountTotalOpenAlertsAsync(ct);

        return new DashboardSummaryDto(
            ActiveTrips: liveUnits.Count(u => u.TripId.HasValue),
            UnitsOnRoute: liveUnits.Count(u => u.LiveStatus is LiveStatus.OnRoute or LiveStatus.Stopped),
            UnitsDeviated: liveUnits.Count(u => u.LiveStatus == LiveStatus.Deviated),
            UnitsNoSignal: liveUnits.Count(u => u.LiveStatus == LiveStatus.NoSignal),
            OpenAlerts: totalOpenAlerts);
    }

    public async Task<TrackDto> GetUnitTrackAsync(
        Guid unitId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        _ = await uow.Units.GetByIdAsync(unitId, ct)
            ?? throw new NotFoundException($"No se encontró la unidad con ID '{unitId}'.");

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var toUtc = (to ?? nowUtc).ToUniversalTime();
        var fromUtc = (from ?? toUtc.AddHours(-12)).ToUniversalTime();

        if (toUtc <= fromUtc)
            throw new ArgumentException("La fecha final ('to') debe ser posterior a la fecha inicial ('from').");

        if ((toUtc - fromUtc) > TimeSpan.FromDays(7))
            throw new ArgumentException("El rango máximo de consulta histórica es de 7 días.");

        var (trips, positions, alerts) = await fleetReadRepository.GetUnitTrackDataAsync(unitId, fromUtc, toUtc, ct);

        var coords = positions.Select(p => p.Location.Coordinate).ToArray();
        GeoJsonLineStringDto pathDto;

        if (coords.Length >= 2)
        {
            var rawLine = GeometryFactory.CreateLineString(coords);
            if (coords.Length > 1500)
            {
                var simplified = DouglasPeuckerSimplifier.Simplify(rawLine, 0.00005) as LineString ?? rawLine;
                pathDto = GeoJsonLineStringDto.FromLineString(simplified);
            }
            else
            {
                pathDto = GeoJsonLineStringDto.FromLineString(rawLine);
            }
        }
        else
        {
            pathDto = new GeoJsonLineStringDto(
                "LineString",
                coords.Select(c => new[] { c.X, c.Y }).ToArray());
        }

        var pointDtos = positions
            .Select(p => new TrackPointDto(
                GeoJsonPointDto.FromPoint(p.Location),
                p.CapturedAt,
                p.SpeedKmh,
                p.AccuracyMeters,
                p.InsideCorridor))
            .ToList();

        var tripDtos = trips
            .Select(t => new TrackTripSummaryDto(
                t.Id,
                t.RouteId,
                t.Route.Name,
                t.StartedAt,
                t.EndedAt))
            .ToList();

        var events = new List<TrackEventDto>();

        foreach (var trip in trips)
        {
            if (trip.StartedAt.HasValue && trip.StartedAt.Value >= fromUtc && trip.StartedAt.Value <= toUtc)
            {
                events.Add(new TrackEventDto(
                    "TripStarted",
                    trip.StartedAt.Value,
                    null,
                    null,
                    $"Inicio de viaje en ruta '{trip.Route.Name}'"));
            }

            if (trip.EndedAt.HasValue && trip.EndedAt.Value >= fromUtc && trip.EndedAt.Value <= toUtc)
            {
                events.Add(new TrackEventDto(
                    "TripFinished",
                    trip.EndedAt.Value,
                    null,
                    null,
                    $"Fin de viaje en ruta '{trip.Route.Name}'"));
            }
        }

        foreach (var alert in alerts)
        {
            events.Add(new TrackEventDto(
                "Alert",
                alert.CreatedAt,
                alert.Location is null ? null : GeoJsonPointDto.FromPoint(alert.Location),
                alert.Id,
                alert.Message));
        }

        // Detectar paradas prolongadas (>= 5 min con velocidad <= 2 km/h)
        Domain.Entities.Position? stopStart = null;
        Domain.Entities.Position? stopLast = null;
        foreach (var pos in positions)
        {
            var isStopped = pos.SpeedKmh is <= 2.0;
            if (isStopped)
            {
                stopStart ??= pos;
                stopLast = pos;
            }
            else
            {
                if (stopStart is not null && stopLast is not null &&
                    (stopLast.CapturedAt - stopStart.CapturedAt) >= TimeSpan.FromMinutes(5))
                {
                    var minutes = Math.Round((stopLast.CapturedAt - stopStart.CapturedAt).TotalMinutes);
                    events.Add(new TrackEventDto(
                        "Stop",
                        stopStart.CapturedAt,
                        GeoJsonPointDto.FromPoint(stopStart.Location),
                        null,
                        $"Unidad detenida durante {minutes} min"));
                }
                stopStart = null;
                stopLast = null;
            }
        }

        if (stopStart is not null && stopLast is not null &&
            (stopLast.CapturedAt - stopStart.CapturedAt) >= TimeSpan.FromMinutes(5))
        {
            var minutes = Math.Round((stopLast.CapturedAt - stopStart.CapturedAt).TotalMinutes);
            events.Add(new TrackEventDto(
                "Stop",
                stopStart.CapturedAt,
                GeoJsonPointDto.FromPoint(stopStart.Location),
                null,
                $"Unidad detenida durante {minutes} min"));
        }

        var sortedEvents = events.OrderBy(e => e.At).ToList();

        return new TrackDto(
            unitId,
            fromUtc,
            toUtc,
            pathDto,
            pointDtos,
            tripDtos,
            sortedEvents);
    }

    public static LiveStatus CalculateLiveStatus(
        bool hasActiveTrip,
        DateTime? lastCapturedAtUtc,
        DateTime nowUtc,
        int noSignalMinutes,
        bool? insideCorridor,
        bool hasOpenDeviationOrZoneAlert,
        double? speedKmh)
    {
        if (!hasActiveTrip)
            return LiveStatus.Idle;

        if (!lastCapturedAtUtc.HasValue ||
            (nowUtc - lastCapturedAtUtc.Value) > TimeSpan.FromMinutes(noSignalMinutes))
        {
            return LiveStatus.NoSignal;
        }

        if (insideCorridor == false || hasOpenDeviationOrZoneAlert)
            return LiveStatus.Deviated;

        if (speedKmh is <= 2.0)
            return LiveStatus.Stopped;

        return LiveStatus.OnRoute;
    }

    private static LiveUnitDto MapSnapshotToDto(UnitLiveSnapshot s, DateTime nowUtc, int noSignalMinutes)
    {
        var status = CalculateLiveStatus(
            hasActiveTrip: s.ActiveTripId.HasValue,
            lastCapturedAtUtc: s.CapturedAt,
            nowUtc: nowUtc,
            noSignalMinutes: noSignalMinutes,
            insideCorridor: s.InsideCorridor,
            hasOpenDeviationOrZoneAlert: s.HasOpenDeviationOrZoneAlert,
            speedKmh: s.SpeedKmh);

        return new LiveUnitDto(
            s.UnitId,
            s.EconomicNumber,
            s.OperatorName,
            s.ActiveTripId,
            s.RouteName,
            status,
            s.LastLocation is null ? null : GeoJsonPointDto.FromPoint(s.LastLocation),
            s.SpeedKmh,
            s.Heading,
            s.AccuracyMeters,
            s.CapturedAt,
            s.OpenAlertsForUnit);
    }
}
