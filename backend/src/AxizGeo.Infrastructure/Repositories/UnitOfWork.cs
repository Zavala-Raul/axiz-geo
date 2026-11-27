using AxizGeo.Application.Abstractions;
using AxizGeo.Infrastructure.Persistence;

namespace AxizGeo.Infrastructure.Repositories;

public class UnitOfWork(
    AppDbContext db,
    IUnitRepository units,
    IOperatorRepository operators,
    IRouteRepository routes,
    IGeofenceRepository geofences,
    ITripRepository trips,
    IPositionRepository positions,
    IAlertRepository alerts) : IUnitOfWork
{
    public IUnitRepository Units => units;
    public IOperatorRepository Operators => operators;
    public IRouteRepository Routes => routes;
    public IGeofenceRepository Geofences => geofences;
    public ITripRepository Trips => trips;
    public IPositionRepository Positions => positions;
    public IAlertRepository Alerts => alerts;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
