using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Rules;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AxizGeo.Application.Services;

public sealed class TelemetryService(
    IUnitOfWork uow,
    TelemetryPipeline pipeline,
    IFleetNotifier fleetNotifier,
    TimeProvider timeProvider,
    IOptions<TelemetryRulesOptions> rulesOptions,
    IValidator<TelemetryBatchRequest> batchValidator)
{
    private readonly TelemetryRulesOptions _options = rulesOptions.Value;

    public async Task<TelemetryBatchResponse> IngestBatchAsync(
        TelemetryBatchRequest request,
        bool isOperatorCaller,
        Guid? callerOperatorId,
        CancellationToken ct = default)
    {
        await batchValidator.ValidateAndThrowAsync(request, ct);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var accepted = 0;
        var duplicates = 0;
        var rejected = new List<RejectedPositionDto>();
        var createdAlerts = new List<Alert>();
        var touchedTrips = new Dictionary<Guid, Trip>();

        var seenIdsInRequest = new HashSet<Guid>();
        var restrictedZones = await uow.Geofences.ListFilteredAsync(GeofenceKind.Restricted, includeInactive: false, ct);

        // Agrupar por viaje para cargar contexto una sola vez por viaje
        var groupedByTrip = request.Positions
            .OrderBy(p => p.CapturedAt)
            .GroupBy(p => p.TripId);

        foreach (var group in groupedByTrip)
        {
            var trip = await uow.Trips.GetWithDetailsAsync(group.Key, ct)
                ?? throw new NotFoundException($"No existe el viaje '{group.Key}'.");

            if (isOperatorCaller && trip.OperatorId != callerOperatorId)
                throw new ForbiddenException("Solo puedes enviar telemetría del viaje asignado a tu operador.");

            if (trip.Status != TripStatus.InProgress)
                throw new ConflictException($"El viaje '{trip.Id}' no está en curso (estado actual: {trip.Status}).");

            var openAlertTypes = (await uow.Alerts.GetOpenAlertTypesAsync(trip.Id, ct)).ToHashSet();
            Position? previousValid = null;
            var firstCandidateTime = group.First().CapturedAt.ToUniversalTime();
            previousValid = await uow.Positions.GetLastValidByTripAsync(
                trip.Id,
                firstCandidateTime,
                _options.MaxAccuracyMeters,
                ct);

            foreach (var item in group)
            {
                if (item.Id == Guid.Empty)
                {
                    rejected.Add(new RejectedPositionDto(item.Id, "El ID de la posición no puede ser vacío."));
                    continue;
                }

                if (!seenIdsInRequest.Add(item.Id) || await uow.Positions.ExistsAsync(item.Id, ct))
                {
                    duplicates++;
                    continue;
                }

                if (item.AccuracyMeters < 0)
                {
                    rejected.Add(new RejectedPositionDto(item.Id, "La precisión ('accuracyMeters') no puede ser negativa."));
                    continue;
                }

                NetTopologySuite.Geometries.Point point;
                try
                {
                    point = GeoJsonMapper.ToPoint(item.Location);
                }
                catch (ArgumentException ex)
                {
                    rejected.Add(new RejectedPositionDto(item.Id, ex.Message));
                    continue;
                }

                var capturedUtc = item.CapturedAt.ToUniversalTime();
                var isLate = (nowUtc - capturedUtc).TotalSeconds > _options.LateThresholdSeconds;

                var position = new Position
                {
                    Id = item.Id,
                    TripId = trip.Id,
                    Trip = trip,
                    Location = point,
                    AccuracyMeters = item.AccuracyMeters,
                    SpeedKmh = item.SpeedKmh,
                    Heading = item.Heading,
                    CapturedAt = capturedUtc,
                    ReceivedAt = nowUtc,
                    Source = item.Source,
                    IsLate = isLate
                };

                var evalContext = new TelemetryEvaluationContext
                {
                    Trip = trip,
                    Position = position,
                    PreviousValidPosition = previousValid,
                    ActiveRestrictedGeofences = restrictedZones,
                    OpenAlertTypesForTrip = openAlertTypes
                };

                await pipeline.ExecuteAsync(evalContext, ct);

                if (evalContext.IsPositionValidForTracking)
                {
                    previousValid = position;
                }

                await uow.Positions.AddAsync(position, ct);
                accepted++;

                foreach (var alert in evalContext.RaisedAlerts)
                {
                    await uow.Alerts.AddAsync(alert, ct);
                    createdAlerts.Add(alert);
                }

                if (trip.LastPositionAt is null || capturedUtc > trip.LastPositionAt.Value)
                {
                    trip.LastPositionAt = capturedUtc;
                    touchedTrips[trip.Id] = trip;
                }
            }
        }

        foreach (var updatedTrip in touchedTrips.Values)
        {
            uow.Trips.Update(updatedTrip);
        }

        if (accepted > 0 || createdAlerts.Count > 0)
        {
            await uow.SaveChangesAsync(ct);
        }

        foreach (var alert in createdAlerts)
        {
            await fleetNotifier.NotifyAlertRaisedAsync(AlertService.MapToDto(alert), ct);
        }

        foreach (var trip in touchedTrips.Values)
        {
            await fleetNotifier.NotifyPositionAcceptedAsync(trip.UnitId, trip.Id, ct);
        }

        return new TelemetryBatchResponse(accepted, duplicates, rejected, createdAlerts.Count);
    }
}
