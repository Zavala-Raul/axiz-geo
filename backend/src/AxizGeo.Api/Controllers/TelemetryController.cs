using AxizGeo.Api.Infrastructure;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Services;
using AxizGeo.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
[Authorize(Roles = "Admin,Operator")]
public sealed class TelemetryController(TelemetryService telemetryService) : ControllerBase
{
    [HttpPost("batch")]
    public async Task<ActionResult<TelemetryBatchResponse>> IngestBatch(
        [FromBody] TelemetryBatchRequest request,
        CancellationToken ct)
    {
        var isOperator = User.IsInRole(nameof(Role.Operator));
        var result = await telemetryService.IngestBatchAsync(request, isOperator, User.GetOperatorId(), ct);
        return Ok(result);
    }
}
