using AxizGeo.Application.Abstractions;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using AxizGeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AxizGeo.Infrastructure.Repositories;

public class UnitRepository(AppDbContext db) : Repository<Unit>(db), IUnitRepository
{
    public Task<bool> ExistsByEconomicNumberAsync(string economicNumber, Guid? excludeId = null, CancellationToken ct = default) =>
        Set.AnyAsync(u => u.EconomicNumber == economicNumber && (excludeId == null || u.Id != excludeId), ct);

    public Task<bool> HasActiveTripAsync(Guid unitId, CancellationToken ct = default) =>
        Db.Trips.AnyAsync(t => t.UnitId == unitId && t.Status == TripStatus.InProgress, ct);

    public async Task<IReadOnlyList<Unit>> ListActiveAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.EconomicNumber).ToListAsync(ct);
}

public class OperatorRepository(AppDbContext db) : Repository<Operator>(db), IOperatorRepository
{
    public Task<Operator?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(o => o.UserId == userId, ct);

    public Task<bool> ExistsByLicenseAsync(string licenseNumber, Guid? excludeId = null, CancellationToken ct = default) =>
        Set.AnyAsync(o => o.LicenseNumber == licenseNumber && (excludeId == null || o.Id != excludeId), ct);

    public Task<bool> HasActiveTripAsync(Guid operatorId, CancellationToken ct = default) =>
        Db.Trips.AnyAsync(t => t.OperatorId == operatorId && t.Status == TripStatus.InProgress, ct);
}

public class RouteRepository(AppDbContext db) : Repository<GeoRoute>(db), IRouteRepository
{
    public Task<bool> HasActiveTripAsync(Guid routeId, CancellationToken ct = default) =>
        Db.Trips.AnyAsync(t => t.RouteId == routeId && t.Status == TripStatus.InProgress, ct);

    public async Task<IReadOnlyList<GeoRoute>> ListActiveAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(r => r.IsActive).OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
}

public class GeofenceRepository(AppDbContext db) : Repository<Geofence>(db), IGeofenceRepository
{
    public async Task<IReadOnlyList<Geofence>> ListFilteredAsync(GeofenceKind? kind, bool includeInactive, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(g => g.IsActive);
        if (kind.HasValue)
            query = query.Where(g => g.Kind == kind.Value);

        return await query.OrderBy(g => g.Name).ToListAsync(ct);
    }
}

public class TripRepository(AppDbContext db) : Repository<Trip>(db), ITripRepository
{
    public Task<Trip?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(t => t.Unit)
           .Include(t => t.Operator)
           .Include(t => t.Route)
           .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Trip?> GetActiveByOperatorAsync(Guid operatorId, CancellationToken ct = default) =>
        Set.Include(t => t.Unit)
           .Include(t => t.Operator)
           .Include(t => t.Route)
           .FirstOrDefaultAsync(t => t.OperatorId == operatorId && t.Status == TripStatus.InProgress, ct);

    public async Task<IReadOnlyList<Trip>> ListByOperatorAsync(Guid operatorId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Include(t => t.Unit)
                 .Include(t => t.Operator)
                 .Include(t => t.Route)
                 .Where(t => t.OperatorId == operatorId &&
                             (t.Status == TripStatus.Scheduled || t.Status == TripStatus.InProgress))
                 .OrderByDescending(t => t.ScheduledStart)
                 .ToListAsync(ct);

    public Task<bool> HasActiveTripForUnitOrOperatorAsync(Guid unitId, Guid operatorId, CancellationToken ct = default) =>
        Set.AnyAsync(t => t.Status == TripStatus.InProgress &&
                          (t.UnitId == unitId || t.OperatorId == operatorId), ct);

    public async Task<IReadOnlyList<Trip>> GetActiveWithoutSignalSinceAsync(DateTime thresholdUtc, CancellationToken ct = default) =>
        await Set.Include(t => t.Unit)
                 .Include(t => t.Operator)
                 .Include(t => t.Route)
                 .Where(t => t.Status == TripStatus.InProgress &&
                             ((t.LastPositionAt != null && t.LastPositionAt < thresholdUtc) ||
                              (t.LastPositionAt == null && t.StartedAt != null && t.StartedAt < thresholdUtc)))
                 .ToListAsync(ct);
}

public class PositionRepository(AppDbContext db) : Repository<Position>(db), IPositionRepository
{
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        Set.AnyAsync(p => p.Id == id, ct);

    public Task<Position?> GetLastValidByTripAsync(Guid tripId, DateTime beforeUtc, double maxAccuracyMeters, CancellationToken ct = default) =>
        Set.AsNoTracking()
           .Where(p => p.TripId == tripId && p.CapturedAt < beforeUtc && p.AccuracyMeters <= maxAccuracyMeters)
           .OrderByDescending(p => p.CapturedAt)
           .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Position>> ListByUnitAndRangeAsync(Guid unitId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Where(p => p.Trip.UnitId == unitId && p.CapturedAt >= fromUtc && p.CapturedAt <= toUtc)
                 .OrderBy(p => p.CapturedAt)
                 .ToListAsync(ct);
}

public class AlertRepository(AppDbContext db) : Repository<Alert>(db), IAlertRepository
{
    public Task<Alert?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(a => a.Trip)
               .ThenInclude(t => t.Unit)
           .Include(a => a.Trip)
               .ThenInclude(t => t.Operator)
           .Include(a => a.Trip)
               .ThenInclude(t => t.Route)
           .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> HasOpenAlertAsync(Guid tripId, AlertType type, CancellationToken ct = default) =>
        Set.AnyAsync(a => a.TripId == tripId &&
                          a.Type == type &&
                          (a.Status == AlertStatus.New || a.Status == AlertStatus.InReview), ct);
}
