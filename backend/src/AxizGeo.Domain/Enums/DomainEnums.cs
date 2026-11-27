namespace AxizGeo.Domain.Enums;

public enum Role
{
    Admin,
    Monitor,
    Operator
}

public enum TripStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public enum GeofenceType
{
    Circle,
    Polygon
}

public enum GeofenceKind
{
    Allowed,
    Restricted
}

public enum AlertType
{
    Deviation,
    NoSignal,
    UnplannedStop,
    RestrictedZone
}

public enum AlertStatus
{
    New,
    InReview,
    Resolved,
    Dismissed
}

public enum AlertSeverity
{
    Low,
    Medium,
    High
}
