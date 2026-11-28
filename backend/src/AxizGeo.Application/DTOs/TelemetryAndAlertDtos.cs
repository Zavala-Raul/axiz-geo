using AxizGeo.Domain.Enums;

namespace AxizGeo.Application.DTOs;

// ==================== TELEMETRY ====================
public sealed record PositionInputDto(
    Guid Id,
    Guid TripId,
    GeoJsonPointDto Location,
    double AccuracyMeters,
    double? SpeedKmh,
    double? Heading,
    DateTime CapturedAt,
    PositionSource Source);

public sealed record TelemetryBatchRequest(IReadOnlyList<PositionInputDto> Positions);

public sealed record RejectedPositionDto(Guid Id, string Reason);

public sealed record TelemetryBatchResponse(
    int Accepted,
    int Duplicates,
    IReadOnlyList<RejectedPositionDto> Rejected,
    int AlertsRaised);

// ==================== ALERTS ====================
public sealed record AlertUnitSummaryDto(Guid Id, string EconomicNumber);
public sealed record AlertTripSummaryDto(Guid Id, string RouteName);

public sealed record AlertDto(
    Guid Id,
    AlertType Type,
    AlertSeverity Severity,
    AlertStatus Status,
    string Message,
    AlertUnitSummaryDto Unit,
    AlertTripSummaryDto Trip,
    string OperatorName,
    GeoJsonPointDto? Location,
    Guid? GeofenceId,
    bool IsHistorical,
    DateTime CreatedAt,
    string? AttendedBy,
    DateTime? AttendedAt,
    string? Resolution);

public sealed record ResolveAlertRequest(string Resolution);
