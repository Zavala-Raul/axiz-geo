using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using NetTopologySuite.Geometries;

namespace AxizGeo.Application.Abstractions;

public sealed record UnitLiveSnapshot(
    Guid UnitId,
    string EconomicNumber,
    Guid? ActiveTripId,
    string? OperatorName,
    string? RouteName,
    Point? LastLocation,
    double? SpeedKmh,
    double? Heading,
    double? AccuracyMeters,
    DateTime? CapturedAt,
    bool? InsideCorridor,
    int OpenAlertsForUnit,
    bool HasOpenDeviationOrZoneAlert);

public interface IFleetReadRepository
{
    Task<IReadOnlyList<UnitLiveSnapshot>> GetFleetSnapshotsAsync(Guid? filterUnitId = null, CancellationToken ct = default);
    Task<int> CountTotalOpenAlertsAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<Trip> Trips, IReadOnlyList<Domain.Entities.Position> Positions, IReadOnlyList<Alert> Alerts)> GetUnitTrackDataAsync(
        Guid unitId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);
}
