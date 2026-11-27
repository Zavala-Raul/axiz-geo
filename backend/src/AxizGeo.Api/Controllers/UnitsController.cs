using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Controllers;

[ApiController]
[Route("api/units")]
[Authorize(Roles = "Admin,Monitor")]
public sealed class UnitsController(UnitService unitService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UnitDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await unitService.ListAsync(search, includeInactive, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitDto>> GetById(Guid id, CancellationToken ct)
    {
        var unit = await unitService.GetByIdAsync(id, ct);
        return Ok(unit);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UnitDto>> Create([FromBody] CreateUnitRequest request, CancellationToken ct)
    {
        var created = await unitService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UnitDto>> Update(Guid id, [FromBody] UpdateUnitRequest request, CancellationToken ct)
    {
        var updated = await unitService.UpdateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await unitService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
