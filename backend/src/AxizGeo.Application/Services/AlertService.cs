using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using FluentValidation;

namespace AxizGeo.Application.Services;

public sealed class AlertService(
    IUnitOfWork uow,
    IFleetNotifier fleetNotifier,
    TimeProvider timeProvider,
    IValidator<ResolveAlertRequest> resolveValidator)
{
    public async Task<PagedResult<AlertDto>> ListAsync(
        IReadOnlyList<AlertStatus>? statuses,
        AlertType? type,
        Guid? unitId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await uow.Alerts.ListPagedAsync(
            statuses, type, unitId, fromUtc, toUtc, page, pageSize, ct);

        return new PagedResult<AlertDto>(items.Select(MapToDto).ToList(), page, pageSize, total);
    }

    public async Task<AlertDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await uow.Alerts.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la alerta con ID '{id}'.");

        return MapToDto(alert);
    }

    public async Task<AlertDto> StartReviewAsync(Guid id, Guid userId, string userFullName, CancellationToken ct = default)
    {
        var alert = await uow.Alerts.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la alerta con ID '{id}'.");

        alert.StartReview(userId, userFullName);
        uow.Alerts.Update(alert);
        await uow.SaveChangesAsync(ct);

        var dto = MapToDto(alert);
        await fleetNotifier.NotifyAlertUpdatedAsync(dto, ct);
        return dto;
    }

    public async Task<AlertDto> ResolveAsync(
        Guid id,
        ResolveAlertRequest request,
        Guid userId,
        string userFullName,
        CancellationToken ct = default)
    {
        await resolveValidator.ValidateAndThrowAsync(request, ct);

        var alert = await uow.Alerts.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la alerta con ID '{id}'.");

        alert.Resolve(userId, userFullName, request.Resolution, timeProvider.GetUtcNow().UtcDateTime);
        uow.Alerts.Update(alert);
        await uow.SaveChangesAsync(ct);

        var dto = MapToDto(alert);
        await fleetNotifier.NotifyAlertUpdatedAsync(dto, ct);
        return dto;
    }

    public async Task<AlertDto> DismissAsync(
        Guid id,
        ResolveAlertRequest request,
        Guid userId,
        string userFullName,
        CancellationToken ct = default)
    {
        await resolveValidator.ValidateAndThrowAsync(request, ct);

        var alert = await uow.Alerts.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la alerta con ID '{id}'.");

        alert.Dismiss(userId, userFullName, request.Resolution, timeProvider.GetUtcNow().UtcDateTime);
        uow.Alerts.Update(alert);
        await uow.SaveChangesAsync(ct);

        var dto = MapToDto(alert);
        await fleetNotifier.NotifyAlertUpdatedAsync(dto, ct);
        return dto;
    }

    public static AlertDto MapToDto(Alert a) => new(
        a.Id,
        a.Type,
        a.Severity,
        a.Status,
        a.Message,
        new AlertUnitSummaryDto(a.Trip.Unit.Id, a.Trip.Unit.EconomicNumber),
        new AlertTripSummaryDto(a.Trip.Id, a.Trip.Route.Name),
        a.Trip.Operator.FullName,
        a.Location is null ? null : GeoJsonPointDto.FromPoint(a.Location),
        a.GeofenceId,
        a.IsHistorical,
        a.CreatedAt,
        a.AttendedByName,
        a.AttendedAt,
        a.Resolution);
}
