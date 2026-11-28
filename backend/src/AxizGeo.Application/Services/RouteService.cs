using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Geofencing;
using AxizGeo.Domain.Entities;
using FluentValidation;

namespace AxizGeo.Application.Services;

public sealed class RouteService(
    IUnitOfWork uow,
    IRoutingProvider routingProvider,
    IValidator<PreviewRouteRequest> previewValidator,
    IValidator<UpsertRouteRequest> upsertValidator)
{
    public async Task<PreviewRouteResponse> PreviewAsync(PreviewRouteRequest request, CancellationToken ct = default)
    {
        await previewValidator.ValidateAndThrowAsync(request, ct);
        _ = GeoJsonMapper.ToMultiPoint(request.Waypoints);

        TruckDimensions? truck = null;
        if (request.UnitId.HasValue)
        {
            var unit = await uow.Units.GetByIdAsync(request.UnitId.Value, ct)
                ?? throw new NotFoundException($"No se encontró la unidad '{request.UnitId}'.");
            truck = new TruckDimensions(unit.HeightMeters, unit.WeightTons);
        }

        var calculated = await routingProvider.CalculateTruckRouteAsync(request.Waypoints, truck, ct);
        return new PreviewRouteResponse(
            GeoJsonLineStringDto.FromLineString(calculated.Path),
            Math.Round(calculated.DistanceMeters, 1),
            Math.Round(calculated.DurationSeconds, 1));
    }

    public async Task<PagedResult<RouteDto>> ListAsync(bool includeInactive, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await uow.Routes.ListPagedAsync(includeInactive, page, pageSize, ct);
        return new PagedResult<RouteDto>(items.Select(MapToDto).ToList(), page, pageSize, total);
    }

    public async Task<RouteDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var route = await uow.Routes.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la ruta con ID '{id}'.");

        return MapToDto(route);
    }

    public async Task<RouteDto> CreateAsync(
        UpsertRouteRequest request,
        Guid userId,
        string userFullName,
        CancellationToken ct = default)
    {
        await upsertValidator.ValidateAndThrowAsync(request, ct);

        var waypoints = GeoJsonMapper.ToMultiPoint(request.Waypoints);
        var path = GeoJsonMapper.ToLineString(request.Path);
        var distance = request.DistanceMeters ?? Math.Round(GeoMath.LineLengthMeters(path), 1);
        var duration = request.DurationSeconds ?? Math.Round(distance / 15.0, 1);

        var route = new GeoRoute
        {
            Name = request.Name.Trim(),
            Waypoints = waypoints,
            Path = path,
            CorridorMeters = request.CorridorMeters,
            DistanceMeters = distance,
            DurationSeconds = duration,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedByName = userFullName
        };

        await uow.Routes.AddAsync(route, ct);
        await uow.SaveChangesAsync(ct);

        return MapToDto(route);
    }

    public async Task<RouteDto> UpdateAsync(
        Guid id,
        UpsertRouteRequest request,
        Guid userId,
        string userFullName,
        CancellationToken ct = default)
    {
        await upsertValidator.ValidateAndThrowAsync(request, ct);

        var route = await uow.Routes.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la ruta con ID '{id}'.");

        if (!request.IsActive && route.IsActive && await uow.Routes.HasActiveTripAsync(id, ct))
            throw new ConflictException("No se puede desactivar una ruta utilizada por un viaje en curso.");

        var waypoints = GeoJsonMapper.ToMultiPoint(request.Waypoints);
        var path = GeoJsonMapper.ToLineString(request.Path);

        route.Name = request.Name.Trim();
        route.Waypoints = waypoints;
        route.Path = path;
        route.CorridorMeters = request.CorridorMeters;
        route.DistanceMeters = request.DistanceMeters ?? Math.Round(GeoMath.LineLengthMeters(path), 1);
        route.DurationSeconds = request.DurationSeconds ?? route.DurationSeconds;
        route.IsActive = request.IsActive;
        route.CreatedByUserId = userId;
        route.CreatedByName = userFullName;

        uow.Routes.Update(route);
        await uow.SaveChangesAsync(ct);

        return MapToDto(route);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var route = await uow.Routes.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la ruta con ID '{id}'.");

        if (await uow.Routes.HasActiveTripAsync(id, ct))
            throw new ConflictException("No se puede eliminar una ruta utilizada por un viaje en curso.");

        route.IsActive = false;
        uow.Routes.Update(route);
        await uow.SaveChangesAsync(ct);
    }

    public static RouteDto MapToDto(GeoRoute r) => new(
        r.Id,
        r.Name,
        GeoJsonMapper.FromMultiPoint(r.Waypoints),
        GeoJsonLineStringDto.FromLineString(r.Path),
        r.CorridorMeters,
        r.DistanceMeters,
        r.DurationSeconds,
        r.IsActive,
        r.CreatedAt,
        r.CreatedByName);
}
