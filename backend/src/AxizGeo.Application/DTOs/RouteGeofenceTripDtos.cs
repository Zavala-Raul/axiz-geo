using System.Text.Json;
using AxizGeo.Domain.Enums;

namespace AxizGeo.Application.DTOs;

// ==================== ROUTES ====================
public sealed record PreviewRouteRequest(
    IReadOnlyList<GeoJsonPointDto> Waypoints,
    Guid? UnitId);

public sealed record PreviewRouteResponse(
    GeoJsonLineStringDto Path,
    double DistanceMeters,
    double DurationSeconds);

public sealed record RouteDto(
    Guid Id,
    string Name,
    IReadOnlyList<GeoJsonPointDto> Waypoints,
    GeoJsonLineStringDto Path,
    int CorridorMeters,
    double DistanceMeters,
    double DurationSeconds,
    bool IsActive,
    DateTime CreatedAt,
    string CreatedBy);

public sealed record UpsertRouteRequest(
    string Name,
    IReadOnlyList<GeoJsonPointDto> Waypoints,
    GeoJsonLineStringDto Path,
    int CorridorMeters,
    double? DistanceMeters = null,
    double? DurationSeconds = null,
    bool IsActive = true);

// ==================== GEOFENCES ====================
public sealed record GeofenceDto(
    Guid Id,
    string Name,
    GeofenceType Type,
    GeofenceKind Kind,
    JsonElement Geometry,
    double? RadiusMeters,
    string? Color,
    bool IsActive,
    DateTime CreatedAt);

public sealed record UpsertGeofenceRequest(
    string Name,
    GeofenceType Type,
    GeofenceKind Kind,
    JsonElement Geometry,
    double? RadiusMeters,
    string? Color,
    bool IsActive = true);

// ==================== TRIPS ====================
public sealed record TripUnitSummaryDto(Guid Id, string EconomicNumber);
public sealed record TripOperatorSummaryDto(Guid Id, string FullName);
public sealed record TripRouteSummaryDto(Guid Id, string Name);

public sealed record TripDto(
    Guid Id,
    TripUnitSummaryDto Unit,
    TripOperatorSummaryDto Operator,
    TripRouteSummaryDto Route,
    TripStatus Status,
    DateTime ScheduledStart,
    DateTime? StartedAt,
    DateTime? EndedAt,
    DateTime? LastPositionAt,
    string? Notes);

public sealed record TripDetailDto(
    Guid Id,
    TripUnitSummaryDto Unit,
    TripOperatorSummaryDto Operator,
    TripRouteSummaryDto Route,
    TripStatus Status,
    DateTime ScheduledStart,
    DateTime? StartedAt,
    DateTime? EndedAt,
    DateTime? LastPositionAt,
    string? Notes,
    RouteDto RouteDetail);

public sealed record CreateTripRequest(
    Guid UnitId,
    Guid OperatorId,
    Guid RouteId,
    DateTime ScheduledStart,
    string? Notes);
