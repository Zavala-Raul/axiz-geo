using AxizGeo.Api.Infrastructure;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Controllers;

[ApiController]
[Route("api/routes")]
[Authorize(Roles = "Admin,Monitor")]
public sealed class RoutesController(RouteService routeService) : ControllerBase
{
    [HttpPost("preview")]
    public async Task<ActionResult<PreviewRouteResponse>> Preview(
        [FromBody] PreviewRouteRequest request,
        CancellationToken ct)
    {
        var preview = await routeService.PreviewAsync(request, ct);
        return Ok(preview);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RouteDto>>> List(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await routeService.ListAsync(includeInactive, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Monitor,Operator")]
    public async Task<ActionResult<RouteDto>> GetById(Guid id, CancellationToken ct)
    {
        var route = await routeService.GetByIdAsync(id, ct);
        return Ok(route);
    }

    [HttpPost]
    public async Task<ActionResult<RouteDto>> Create([FromBody] UpsertRouteRequest request, CancellationToken ct)
    {
        var created = await routeService.CreateAsync(request, User.GetUserId(), User.GetUserFullName(), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RouteDto>> Update(
        Guid id,
        [FromBody] UpsertRouteRequest request,
        CancellationToken ct)
    {
        var updated = await routeService.UpdateAsync(id, request, User.GetUserId(), User.GetUserFullName(), ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await routeService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
