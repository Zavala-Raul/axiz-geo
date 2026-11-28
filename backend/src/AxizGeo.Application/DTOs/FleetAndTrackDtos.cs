using AxizGeo.Domain.Enums;

namespace AxizGeo.Application.DTOs;

public sealed record LiveUnitDto(
    Guid UnitId,
    string EconomicNumber,
    string? OperatorName,
    Guid? TripId,
    string? RouteName,
    LiveStatus LiveStatus,
    GeoJsonPointDto? Location,
    double? SpeedKmh,
    double? Heading,
    double? AccuracyMeters,
    DateTime? CapturedAt,
    int OpenAlerts);

public sealed record DashboardSummaryDto(
    int ActiveTrips,
    int UnitsOnRoute,
    int UnitsDeviated,
    int UnitsNoSignal,
    int OpenAlerts);

public sealed record TrackPointDto(
    GeoJsonPointDto Location,
    DateTime CapturedAt,
    double? SpeedKmh,
    double AccuracyMeters,
    bool? InsideCorridor);

public sealed record TrackTripSummaryDto(
    Guid Id,
    Guid RouteId,
    string RouteName,
    DateTime? StartedAt,
    DateTime? EndedAt);

public sealed record TrackEventDto(
    string Type,
    DateTime At,
    GeoJsonPointDto? Location,
    Guid? AlertId,
    string Description);

public sealed record TrackDto(
    Guid UnitId,
    DateTime From,
    DateTime To,
    GeoJsonLineStringDto Path,
    IReadOnlyList<TrackPointDto> Points,
    IReadOnlyList<TrackTripSummaryDto> Trips,
    IReadOnlyList<TrackEventDto> Events);
