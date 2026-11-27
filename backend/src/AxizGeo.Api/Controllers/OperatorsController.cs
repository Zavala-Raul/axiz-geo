using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Controllers;

[ApiController]
[Route("api/operators")]
[Authorize(Roles = "Admin,Monitor")]
public sealed class OperatorsController(OperatorService operatorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OperatorDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await operatorService.ListAsync(search, includeInactive, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OperatorDto>> GetById(Guid id, CancellationToken ct)
    {
        var op = await operatorService.GetByIdAsync(id, ct);
        return Ok(op);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OperatorDto>> Create([FromBody] CreateOperatorRequest request, CancellationToken ct)
    {
        var created = await operatorService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OperatorDto>> Update(Guid id, [FromBody] UpdateOperatorRequest request, CancellationToken ct)
    {
        var updated = await operatorService.UpdateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await operatorService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
