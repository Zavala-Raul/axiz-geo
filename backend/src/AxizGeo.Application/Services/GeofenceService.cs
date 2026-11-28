using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using FluentValidation;

namespace AxizGeo.Application.Services;

public sealed class GeofenceService(
    IUnitOfWork uow,
    IValidator<UpsertGeofenceRequest> validator)
{
    public async Task<IReadOnlyList<GeofenceDto>> ListAsync(
        GeofenceKind? kind,
        bool includeInactive,
        CancellationToken ct = default)
    {
        var list = await uow.Geofences.ListFilteredAsync(kind, includeInactive, ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task<GeofenceDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var geofence = await uow.Geofences.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la geocerca con ID '{id}'.");

        return MapToDto(geofence);
    }

    public async Task<GeofenceDto> CreateAsync(UpsertGeofenceRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var geometry = GeoJsonMapper.ParseGeofenceGeometry(request.Type, request.Geometry);

        var geofence = new Geofence
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Kind = request.Kind,
            Geometry = geometry,
            RadiusMeters = request.Type == GeofenceType.Circle ? request.RadiusMeters : null,
            Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim(),
            IsActive = request.IsActive
        };

        await uow.Geofences.AddAsync(geofence, ct);
        await uow.SaveChangesAsync(ct);

        return MapToDto(geofence);
    }

    public async Task<GeofenceDto> UpdateAsync(Guid id, UpsertGeofenceRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var geofence = await uow.Geofences.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la geocerca con ID '{id}'.");

        var geometry = GeoJsonMapper.ParseGeofenceGeometry(request.Type, request.Geometry);

        geofence.Name = request.Name.Trim();
        geofence.Type = request.Type;
        geofence.Kind = request.Kind;
        geofence.Geometry = geometry;
        geofence.RadiusMeters = request.Type == GeofenceType.Circle ? request.RadiusMeters : null;
        geofence.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim();
        geofence.IsActive = request.IsActive;

        uow.Geofences.Update(geofence);
        await uow.SaveChangesAsync(ct);

        return MapToDto(geofence);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var geofence = await uow.Geofences.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la geocerca con ID '{id}'.");

        geofence.IsActive = false;
        uow.Geofences.Update(geofence);
        await uow.SaveChangesAsync(ct);
    }

    private static GeofenceDto MapToDto(Geofence g) => new(
        g.Id,
        g.Name,
        g.Type,
        g.Kind,
        GeoJsonMapper.ToJsonElement(g.Geometry),
        g.RadiusMeters,
        g.Color,
        g.IsActive,
        g.CreatedAt);
}
