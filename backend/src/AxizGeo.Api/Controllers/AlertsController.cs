using AxizGeo.Api.Infrastructure;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using AxizGeo.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize(Roles = "Admin,Monitor")]
public sealed class AlertsController(AlertService alertService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AlertDto>>> List(
        [FromQuery] string? status = null,
        [FromQuery] AlertType? type = null,
        [FromQuery] Guid? unitId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        IReadOnlyList<AlertStatus>? parsedStatuses = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            var list = new List<AlertStatus>();
            foreach (var raw in status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Enum.TryParse<AlertStatus>(raw, ignoreCase: true, out var parsed))
                    throw new ArgumentException($"Estado de alerta no válido: '{raw}'.");
                list.Add(parsed);
            }
            parsedStatuses = list;
        }

        var result = await alertService.ListAsync(parsedStatuses, type, unitId, from, to, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlertDto>> GetById(Guid id, CancellationToken ct)
    {
        var alert = await alertService.GetByIdAsync(id, ct);
        return Ok(alert);
    }

    [HttpPost("{id:guid}/review")]
    public async Task<ActionResult<AlertDto>> Review(Guid id, CancellationToken ct)
    {
        var updated = await alertService.StartReviewAsync(id, User.GetUserId(), User.GetUserFullName(), ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<AlertDto>> Resolve(
        Guid id,
        [FromBody] ResolveAlertRequest request,
        CancellationToken ct)
    {
        var updated = await alertService.ResolveAsync(id, request, User.GetUserId(), User.GetUserFullName(), ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/dismiss")]
    public async Task<ActionResult<AlertDto>> Dismiss(
        Guid id,
        [FromBody] ResolveAlertRequest request,
        CancellationToken ct)
    {
        var updated = await alertService.DismissAsync(id, request, User.GetUserId(), User.GetUserFullName(), ct);
        return Ok(updated);
    }
}
