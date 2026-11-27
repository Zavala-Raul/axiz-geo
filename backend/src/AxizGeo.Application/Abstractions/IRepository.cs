using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;

namespace AxizGeo.Application.Abstractions;

/// <summary>
/// Patrón Repository genérico para desacoplar la lógica de negocio del ORM (EF Core).
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitRepository : IRepository<Unit>
{
    Task<bool> ExistsByEconomicNumberAsync(string economicNumber, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> HasActiveTripAsync(Guid unitId, CancellationToken ct = default);
    Task<IReadOnlyList<Unit>> ListActiveAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<Unit> Items, int TotalCount)> ListPagedAsync(string? search, bool includeInactive, int page, int pageSize, CancellationToken ct = default);
}

public interface IOperatorRepository : IRepository<Operator>
{
    Task<Operator?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsByLicenseAsync(string licenseNumber, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> HasActiveTripAsync(Guid operatorId, CancellationToken ct = default);
    Task<(IReadOnlyList<Operator> Items, int TotalCount)> ListPagedAsync(string? search, bool includeInactive, int page, int pageSize, CancellationToken ct = default);
}

public interface IRouteRepository : IRepository<GeoRoute>
{
    Task<bool> HasActiveTripAsync(Guid routeId, CancellationToken ct = default);
    Task<IReadOnlyList<GeoRoute>> ListActiveAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<GeoRoute> Items, int TotalCount)> ListPagedAsync(bool includeInactive, int page, int pageSize, CancellationToken ct = default);
}

public interface IGeofenceRepository : IRepository<Geofence>
{
    Task<IReadOnlyList<Geofence>> ListFilteredAsync(GeofenceKind? kind, bool includeInactive, CancellationToken ct = default);
}

public interface ITripRepository : IRepository<Trip>
{
    Task<Trip?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<Trip?> GetActiveByOperatorAsync(Guid operatorId, CancellationToken ct = default);
    Task<IReadOnlyList<Trip>> ListByOperatorAsync(Guid operatorId, CancellationToken ct = default);
    Task<bool> HasActiveTripForUnitOrOperatorAsync(Guid unitId, Guid operatorId, CancellationToken ct = default);
    Task<IReadOnlyList<Trip>> GetActiveWithoutSignalSinceAsync(DateTime thresholdUtc, CancellationToken ct = default);
    Task<(IReadOnlyList<Trip> Items, int TotalCount)> ListPagedAsync(
        TripStatus? status,
        Guid? unitId,
        Guid? operatorId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

public interface IPositionRepository : IRepository<Position>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<Position?> GetLastValidByTripAsync(Guid tripId, DateTime beforeUtc, double maxAccuracyMeters, CancellationToken ct = default);
    Task<IReadOnlyList<Position>> ListByUnitAndRangeAsync(Guid unitId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}

public interface IAlertRepository : IRepository<Alert>
{
    Task<Alert?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasOpenAlertAsync(Guid tripId, AlertType type, CancellationToken ct = default);
}

/// <summary>
/// Patrón Unit of Work: agrupa repositorios y garantiza atomicidad transaccional en un solo SaveChangesAsync.
/// </summary>
public interface IUnitOfWork
{
    IUnitRepository Units { get; }
    IOperatorRepository Operators { get; }
    IRouteRepository Routes { get; }
    IGeofenceRepository Geofences { get; }
    ITripRepository Trips { get; }
    IPositionRepository Positions { get; }
    IAlertRepository Alerts { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
