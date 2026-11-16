using AssetManagement.Api.Authorization;
using AssetManagement.Api.Extensions;
using AssetManagement.Application.Maintenance;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers;

/// <summary>Maintenance and calibration records for an asset.</summary>
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}/assets/{assetId:guid}/maintenance")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenance;

    public MaintenanceController(IMaintenanceService maintenance) => _maintenance = maintenance;

    /// <summary>Lists maintenance records for an asset, newest first.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.CanView)]
    [ProducesResponseType<IReadOnlyList<MaintenanceRecordDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MaintenanceRecordDto>>> History(
        Guid assetId,
        CancellationToken cancellationToken) =>
        Ok(await _maintenance.GetHistoryAsync(assetId, cancellationToken));

    /// <summary>Starts maintenance; the asset becomes unavailable for checkout until completed.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CanOperate)]
    [ProducesResponseType<MaintenanceRecordDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRecordDto>> Start(
        Guid assetId,
        StartMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _maintenance.StartAsync(assetId, User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(History), new { assetId, version = ApiVersions.V1Route }, record);
    }

    /// <summary>Completes maintenance and returns the asset to service.</summary>
    [HttpPost("{recordId:guid}/complete")]
    [Authorize(Policy = Policies.CanOperate)]
    [ProducesResponseType<MaintenanceRecordDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRecordDto>> Complete(
        Guid assetId,
        Guid recordId,
        CompleteMaintenanceRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _maintenance.CompleteAsync(assetId, recordId, request, cancellationToken));
}
