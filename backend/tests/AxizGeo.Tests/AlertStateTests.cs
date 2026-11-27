using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using AxizGeo.Domain.States;

namespace AxizGeo.Tests;

public class AlertStateTests
{
    [Fact]
    public void NewAlert_CanTransitionTo_InReview_Then_Resolved()
    {
        var alert = new Alert
        {
            TripId = Guid.NewGuid(),
            Type = AlertType.Deviation,
            Severity = AlertSeverity.High,
            Message = "Unidad T-101 fuera del corredor autorizado"
        };

        var monitorId = Guid.NewGuid();
        alert.StartReview(monitorId, "Monitor Demo");
        Assert.Equal(AlertStatus.InReview, alert.Status);

        alert.Resolve(monitorId, "Monitor Demo", "Desvío autorizado por cierre vial en Mex II", DateTime.UtcNow);
        Assert.Equal(AlertStatus.Resolved, alert.Status);
        Assert.NotNull(alert.AttendedAt);
    }

    [Fact]
    public void ResolvedAlert_ThrowsOn_StartReview()
    {
        var state = AlertState.From(AlertStatus.Resolved);

        Assert.Throws<InvalidAlertTransitionException>(() => state.StartReview());
    }

    [Fact]
    public void Trip_Lifecycle_EnforcesValidTransitions()
    {
        var trip = new Trip
        {
            UnitId = Guid.NewGuid(),
            OperatorId = Guid.NewGuid(),
            RouteId = Guid.NewGuid(),
            ScheduledStart = DateTime.UtcNow
        };

        trip.Start(DateTime.UtcNow);
        Assert.Equal(TripStatus.InProgress, trip.Status);

        trip.Finish(DateTime.UtcNow.AddMinutes(30));
        Assert.Equal(TripStatus.Completed, trip.Status);

        Assert.Throws<InvalidTripTransitionException>(() => trip.Cancel());
    }
}
