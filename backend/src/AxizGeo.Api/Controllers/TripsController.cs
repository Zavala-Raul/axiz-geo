using AxizGeo.Api.Infrastructure;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using AxizGeo.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]
public sealed class TripsController(TripService tripService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Monitor")]
    public async Task<ActionResult<PagedResult<TripDto>>> List(
        [FromQuery] TripStatus? status = null,
        [FromQuery] Guid? unitId = null,
        [FromQuery] Guid? operatorId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await tripService.ListAsync(status, unitId, operatorId, from, to, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<IReadOnlyList<TripDto>>> ListMine(CancellationToken ct)
    {
        var operatorId = User.GetOperatorId()
            ?? throw new ForbiddenException("Tu usuario no tiene un perfil de operador asociado.");

        var trips = await tripService.ListMineAsync(operatorId, ct);
        return Ok(trips);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Monitor,Operator")]
    public async Task<ActionResult<TripDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var isOperator = User.IsInRole(nameof(Role.Operator));
        var trip = await tripService.GetByIdAsync(id, isOperator, User.GetOperatorId(), ct);
        return Ok(trip);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Monitor")]
    public async Task<ActionResult<TripDto>> Create([FromBody] CreateTripRequest request, CancellationToken ct)
    {
        var created = await tripService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<TripDto>> Start(Guid id, CancellationToken ct)
    {
        var isOperator = User.IsInRole(nameof(Role.Operator));
        var started = await tripService.StartAsync(id, isOperator, User.GetOperatorId(), ct);
        return Ok(started);
    }

    [HttpPost("{id:guid}/finish")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<TripDto>> Finish(Guid id, CancellationToken ct)
    {
        var isOperator = User.IsInRole(nameof(Role.Operator));
        var finished = await tripService.FinishAsync(id, isOperator, User.GetOperatorId(), ct);
        return Ok(finished);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Monitor")]
    public async Task<ActionResult<TripDto>> Cancel(Guid id, CancellationToken ct)
    {
        var cancelled = await tripService.CancelAsync(id, ct);
        return Ok(cancelled);
    }
}
