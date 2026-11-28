using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using FluentValidation;

namespace AxizGeo.Application.Services;

public sealed class TripService(
    IUnitOfWork uow,
    TimeProvider timeProvider,
    IValidator<CreateTripRequest> createValidator)
{
    public async Task<PagedResult<TripDto>> ListAsync(
        TripStatus? status,
        Guid? unitId,
        Guid? operatorId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await uow.Trips.ListPagedAsync(
            status, unitId, operatorId, fromUtc, toUtc, page, pageSize, ct);

        return new PagedResult<TripDto>(items.Select(MapToDto).ToList(), page, pageSize, total);
    }

    public async Task<IReadOnlyList<TripDto>> ListMineAsync(Guid operatorId, CancellationToken ct = default)
    {
        var items = await uow.Trips.ListByOperatorAsync(operatorId, ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<TripDetailDto> GetByIdAsync(
        Guid id,
        bool isOperatorCaller,
        Guid? callerOperatorId,
        CancellationToken ct = default)
    {
        var trip = await uow.Trips.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró el viaje con ID '{id}'.");

        if (isOperatorCaller && trip.OperatorId != callerOperatorId)
            throw new ForbiddenException("No tienes permiso para consultar un viaje asignado a otro operador.");

        return new TripDetailDto(
            trip.Id,
            new TripUnitSummaryDto(trip.Unit.Id, trip.Unit.EconomicNumber),
            new TripOperatorSummaryDto(trip.Operator.Id, trip.Operator.FullName),
            new TripRouteSummaryDto(trip.Route.Id, trip.Route.Name),
            trip.Status,
            trip.ScheduledStart,
            trip.StartedAt,
            trip.EndedAt,
            trip.LastPositionAt,
            trip.Notes,
            RouteService.MapToDto(trip.Route));
    }

    public async Task<TripDto> CreateAsync(CreateTripRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var unit = await uow.Units.GetByIdAsync(request.UnitId, ct)
            ?? throw new NotFoundException($"No se encontró la unidad '{request.UnitId}'.");
        if (!unit.IsActive)
            throw new ConflictException("La unidad seleccionada está inactiva.");

        var op = await uow.Operators.GetByIdAsync(request.OperatorId, ct)
            ?? throw new NotFoundException($"No se encontró el operador '{request.OperatorId}'.");
        if (!op.IsActive)
            throw new ConflictException("El operador seleccionado está inactivo.");

        var route = await uow.Routes.GetByIdAsync(request.RouteId, ct)
            ?? throw new NotFoundException($"No se encontró la ruta '{request.RouteId}'.");
        if (!route.IsActive)
            throw new ConflictException("La ruta seleccionada está inactiva.");

        var trip = new Trip
        {
            UnitId = unit.Id,
            OperatorId = op.Id,
            RouteId = route.Id,
            Status = TripStatus.Scheduled,
            ScheduledStart = request.ScheduledStart.ToUniversalTime(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        await uow.Trips.AddAsync(trip, ct);
        await uow.SaveChangesAsync(ct);

        trip.Unit = unit;
        trip.Operator = op;
        trip.Route = route;
        return MapToDto(trip);
    }

    public async Task<TripDto> StartAsync(
        Guid id,
        bool isOperatorCaller,
        Guid? callerOperatorId,
        CancellationToken ct = default)
    {
        var trip = await uow.Trips.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró el viaje con ID '{id}'.");

        if (isOperatorCaller && trip.OperatorId != callerOperatorId)
            throw new ForbiddenException("Solo puedes iniciar viajes asignados a tu cuenta de operador.");

        if (await uow.Trips.HasActiveTripForUnitOrOperatorAsync(trip.UnitId, trip.OperatorId, ct))
            throw new ConflictException("La unidad o el operador ya cuentan con otro viaje en curso.");

        trip.Start(timeProvider.GetUtcNow().UtcDateTime);
        uow.Trips.Update(trip);
        await uow.SaveChangesAsync(ct);

        return MapToDto(trip);
    }

    public async Task<TripDto> FinishAsync(
        Guid id,
        bool isOperatorCaller,
        Guid? callerOperatorId,
        CancellationToken ct = default)
    {
        var trip = await uow.Trips.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró el viaje con ID '{id}'.");

        if (isOperatorCaller && trip.OperatorId != callerOperatorId)
            throw new ForbiddenException("Solo puedes finalizar viajes asignados a tu cuenta de operador.");

        trip.Finish(timeProvider.GetUtcNow().UtcDateTime);
        uow.Trips.Update(trip);
        await uow.SaveChangesAsync(ct);

        return MapToDto(trip);
    }

    public async Task<TripDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var trip = await uow.Trips.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró el viaje con ID '{id}'.");

        trip.Cancel();
        uow.Trips.Update(trip);
        await uow.SaveChangesAsync(ct);

        return MapToDto(trip);
    }

    public static TripDto MapToDto(Trip t) => new(
        t.Id,
        new TripUnitSummaryDto(t.Unit.Id, t.Unit.EconomicNumber),
        new TripOperatorSummaryDto(t.Operator.Id, t.Operator.FullName),
        new TripRouteSummaryDto(t.Route.Id, t.Route.Name),
        t.Status,
        t.ScheduledStart,
        t.StartedAt,
        t.EndedAt,
        t.LastPositionAt,
        t.Notes);
}
