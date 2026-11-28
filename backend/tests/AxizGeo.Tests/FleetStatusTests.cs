using AxizGeo.Application.Services;
using AxizGeo.Domain.Enums;

namespace AxizGeo.Tests;

public class FleetStatusTests
{
    [Fact]
    public void CalculateLiveStatus_ReturnsIdle_WhenNoActiveTrip()
    {
        var status = FleetQueryService.CalculateLiveStatus(
            hasActiveTrip: false,
            lastCapturedAtUtc: DateTime.UtcNow,
            nowUtc: DateTime.UtcNow,
            noSignalMinutes: 3,
            insideCorridor: true,
            hasOpenDeviationOrZoneAlert: false,
            speedKmh: 65);

        Assert.Equal(LiveStatus.Idle, status);
    }

    [Fact]
    public void CalculateLiveStatus_ReturnsNoSignal_WhenReportOlderThanThreshold()
    {
        var now = DateTime.UtcNow;
        var status = FleetQueryService.CalculateLiveStatus(
            hasActiveTrip: true,
            lastCapturedAtUtc: now.AddMinutes(-5),
            nowUtc: now,
            noSignalMinutes: 3,
            insideCorridor: true,
            hasOpenDeviationOrZoneAlert: false,
            speedKmh: 70);

        Assert.Equal(LiveStatus.NoSignal, status);
    }

    [Fact]
    public void CalculateLiveStatus_ReturnsDeviated_WhenOutsideCorridor()
    {
        var now = DateTime.UtcNow;
        var status = FleetQueryService.CalculateLiveStatus(
            hasActiveTrip: true,
            lastCapturedAtUtc: now.AddSeconds(-20),
            nowUtc: now,
            noSignalMinutes: 3,
            insideCorridor: false,
            hasOpenDeviationOrZoneAlert: false,
            speedKmh: 55);

        Assert.Equal(LiveStatus.Deviated, status);
    }

    [Fact]
    public void CalculateLiveStatus_ReturnsStoppedOrOnRoute_AccordingToSpeed()
    {
        var now = DateTime.UtcNow;

        var stopped = FleetQueryService.CalculateLiveStatus(
            hasActiveTrip: true,
            lastCapturedAtUtc: now.AddSeconds(-15),
            nowUtc: now,
            noSignalMinutes: 3,
            insideCorridor: true,
            hasOpenDeviationOrZoneAlert: false,
            speedKmh: 0.5);

        var moving = FleetQueryService.CalculateLiveStatus(
            hasActiveTrip: true,
            lastCapturedAtUtc: now.AddSeconds(-15),
            nowUtc: now,
            noSignalMinutes: 3,
            insideCorridor: true,
            hasOpenDeviationOrZoneAlert: false,
            speedKmh: 72.0);

        Assert.Equal(LiveStatus.Stopped, stopped);
        Assert.Equal(LiveStatus.OnRoute, moving);
    }
}
