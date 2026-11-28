using AxizGeo.Application.Abstractions;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using AxizGeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AxizGeo.Infrastructure.Repositories;

public sealed class FleetReadRepository(AppDbContext db) : IFleetReadRepository
{
    public async Task<IReadOnlyList<UnitLiveSnapshot>> GetFleetSnapshotsAsync(
        Guid? filterUnitId = null,
        CancellationToken ct = default)
    {
        var unitsQuery = db.Units.AsNoTracking().Where(u => u.IsActive);
        if (filterUnitId.HasValue)
            unitsQuery = unitsQuery.Where(u => u.Id == filterUnitId.Value);

        var units = await unitsQuery
            .OrderBy(u => u.EconomicNumber)
            .Select(u => new { u.Id, u.EconomicNumber })
            .ToListAsync(ct);

        if (units.Count == 0)
            return [];

        var unitIds = units.Select(u => u.Id).ToList();

        var activeTrips = await db.Trips.AsNoTracking()
            .Include(t => t.Operator)
            .Include(t => t.Route)
            .Where(t => unitIds.Contains(t.UnitId) && t.Status == TripStatus.InProgress)
            .ToListAsync(ct);

        var activeTripIds = activeTrips.Select(t => t.Id).ToList();

        // Una sola consulta agrupada para la posición más reciente de cada viaje activo
        var latestPositionsByTrip = new Dictionary<Guid, Domain.Entities.Position>();
        if (activeTripIds.Count > 0)
        {
            var latestPositions = await db.Positions.AsNoTracking()
                .Where(p => activeTripIds.Contains(p.TripId))
                .GroupBy(p => p.TripId)
                .Select(g => g.OrderByDescending(p => p.CapturedAt).First())
                .ToListAsync(ct);

            latestPositionsByTrip = latestPositions.ToDictionary(p => p.TripId);
        }

        // Alertas abiertas (New / InReview) asociadas a las unidades consultadas
        var openAlerts = await db.Alerts.AsNoTracking()
            .Where(a => unitIds.Contains(a.Trip.UnitId) &&
                        (a.Status == AlertStatus.New || a.Status == AlertStatus.InReview))
            .Select(a => new { a.Trip.UnitId, a.TripId, a.Type })
            .ToListAsync(ct);

        var openCountByUnit = openAlerts
            .GroupBy(a => a.UnitId)
            .ToDictionary(g => g.Key, g => g.Count());

        var deviationTripIds = openAlerts
            .Where(a => a.Type is AlertType.Deviation or AlertType.RestrictedZone)
            .Select(a => a.TripId)
            .ToHashSet();

        var activeTripByUnit = activeTrips.ToDictionary(t => t.UnitId);

        var snapshots = new List<UnitLiveSnapshot>(units.Count);
        foreach (var unit in units)
        {
            activeTripByUnit.TryGetValue(unit.Id, out var trip);
            Domain.Entities.Position? latestPos = null;
            if (trip is not null)
            {
                latestPositionsByTrip.TryGetValue(trip.Id, out latestPos);
            }

            openCountByUnit.TryGetValue(unit.Id, out var openCount);

            snapshots.Add(new UnitLiveSnapshot(
                UnitId: unit.Id,
                EconomicNumber: unit.EconomicNumber,
                ActiveTripId: trip?.Id,
                OperatorName: trip?.Operator.FullName,
                RouteName: trip?.Route.Name,
                LastLocation: latestPos?.Location,
                SpeedKmh: latestPos?.SpeedKmh,
                Heading: latestPos?.Heading,
                AccuracyMeters: latestPos?.AccuracyMeters,
                CapturedAt: latestPos?.CapturedAt ?? trip?.LastPositionAt,
                InsideCorridor: latestPos?.InsideCorridor,
                OpenAlertsForUnit: openCount,
                HasOpenDeviationOrZoneAlert: trip is not null && deviationTripIds.Contains(trip.Id)));
        }

        return snapshots;
    }

    public Task<int> CountTotalOpenAlertsAsync(CancellationToken ct = default) =>
        db.Alerts.CountAsync(a => a.Status == AlertStatus.New || a.Status == AlertStatus.InReview, ct);

    public async Task<(IReadOnlyList<Trip> Trips, IReadOnlyList<Domain.Entities.Position> Positions, IReadOnlyList<Alert> Alerts)> GetUnitTrackDataAsync(
        Guid unitId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        var trips = await db.Trips.AsNoTracking()
            .Include(t => t.Route)
            .Where(t => t.UnitId == unitId &&
                        t.Status != TripStatus.Cancelled &&
                        ((t.StartedAt != null && t.StartedAt <= toUtc && (t.EndedAt == null || t.EndedAt >= fromUtc)) ||
                         (t.ScheduledStart >= fromUtc && t.ScheduledStart <= toUtc)))
            .OrderBy(t => t.StartedAt ?? t.ScheduledStart)
            .ToListAsync(ct);

        var positions = await db.Positions.AsNoTracking()
            .Where(p => p.Trip.UnitId == unitId && p.CapturedAt >= fromUtc && p.CapturedAt <= toUtc)
            .OrderBy(p => p.CapturedAt)
            .ToListAsync(ct);

        var alerts = await db.Alerts.AsNoTracking()
            .Where(a => a.Trip.UnitId == unitId && a.CreatedAt >= fromUtc && a.CreatedAt <= toUtc)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        return (trips, positions, alerts);
    }
}
