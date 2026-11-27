using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using AxizGeo.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Controllers;

[ApiController]
[Route("api/geofences")]
[Authorize(Roles = "Admin,Monitor")]
public sealed class GeofencesController(GeofenceService geofenceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GeofenceDto>>> List(
        [FromQuery] GeofenceKind? kind = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var list = await geofenceService.ListAsync(kind, includeInactive, ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GeofenceDto>> GetById(Guid id, CancellationToken ct)
    {
        var geofence = await geofenceService.GetByIdAsync(id, ct);
        return Ok(geofence);
    }

    [HttpPost]
    public async Task<ActionResult<GeofenceDto>> Create([FromBody] UpsertGeofenceRequest request, CancellationToken ct)
    {
        var created = await geofenceService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GeofenceDto>> Update(
        Guid id,
        [FromBody] UpsertGeofenceRequest request,
        CancellationToken ct)
    {
        var updated = await geofenceService.UpdateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await geofenceService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
