using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Controllers;

[ApiController]
[Route("api/fleet")]
[Authorize(Roles = "Admin,Monitor")]
public sealed class FleetController(FleetQueryService fleetQueryService) : ControllerBase
{
    [HttpGet("live")]
    public async Task<ActionResult<IReadOnlyList<LiveUnitDto>>> GetLive(CancellationToken ct)
    {
        var live = await fleetQueryService.GetLiveFleetAsync(ct);
        return Ok(live);
    }
}
