using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Alerts;
using AxizGeo.Application.Rules;
using AxizGeo.Application.Services;
using AxizGeo.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AxizGeo.Api.BackgroundJobs;

public sealed class NoSignalMonitorBackgroundService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<TelemetryRulesOptions> rulesOptions,
    ILogger<NoSignalMonitorBackgroundService> logger) : BackgroundService
{
    private readonly TelemetryRulesOptions _options = rulesOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckStaleTripsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Omitiendo ciclo de verificación de señal (base de datos aún no disponible o en reconexión).");
            }
        }
    }

    private async Task CheckStaleTripsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var alertFactory = scope.ServiceProvider.GetRequiredService<AlertFactory>();
        var notifier = scope.ServiceProvider.GetRequiredService<IFleetNotifier>();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var thresholdUtc = nowUtc.AddMinutes(-_options.NoSignalMinutes);

        var staleTrips = await uow.Trips.GetActiveWithoutSignalSinceAsync(thresholdUtc, ct);
        if (staleTrips.Count == 0)
            return;

        var raisedAlerts = new List<Domain.Entities.Alert>();

        foreach (var trip in staleTrips)
        {
            if (await uow.Alerts.HasOpenAlertAsync(trip.Id, AlertType.NoSignal, ct))
                continue;

            var alert = alertFactory.CreateNoSignalAlert(trip, _options.NoSignalMinutes);
            await uow.Alerts.AddAsync(alert, ct);
            raisedAlerts.Add(alert);
        }

        if (raisedAlerts.Count == 0)
            return;

        await uow.SaveChangesAsync(ct);

        foreach (var alert in raisedAlerts)
        {
            await notifier.NotifyAlertRaisedAsync(AlertService.MapToDto(alert), ct);
        }
    }
}
