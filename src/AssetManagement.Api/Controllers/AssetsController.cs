using AssetManagement.Api.Authorization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers;

/// <summary>The equipment register.</summary>
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}/assets")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetService _assets;

    public AssetsController(IAssetService assets) => _assets = assets;

    /// <summary>Lists assets with paging, filtering and sorting.</summary>
    /// <remarks>
    /// Example: <c>GET /api/v1/assets?type=Drone&amp;status=Available&amp;search=matrice&amp;sortBy=name&amp;sortDirection=Desc&amp;page=1&amp;pageSize=20</c>.
    /// Retired assets are excluded unless <c>includeRetired=true</c> or <c>status=Retired</c>.
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = Policies.CanView)]
    [ProducesResponseType<PagedResult<AssetDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AssetDto>>> List(
        [FromQuery] AssetQueryParameters query,
        CancellationToken cancellationToken) =>
        Ok(await _assets.ListAsync(query, cancellationToken));

    /// <summary>Gets a single asset.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.CanView)]
    [ProducesResponseType<AssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _assets.GetAsync(id, cancellationToken));

    /// <summary>Registers a new asset. The type decides which specification field is required.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CanAdminister)]
    [ProducesResponseType<AssetDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDto>> Create(CreateAssetRequest request, CancellationToken cancellationToken)
    {
        var asset = await _assets.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = asset.Id, version = ApiVersions.V1Route }, asset);
    }

    /// <summary>Updates an asset's descriptive details and specifications.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanAdminister)]
    [ProducesResponseType<AssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDto>> Update(
        Guid id,
        UpdateAssetRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _assets.UpdateAsync(id, request, cancellationToken));

    /// <summary>Retires an asset (soft delete). History is preserved.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanAdminister)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retire(Guid id, CancellationToken cancellationToken)
    {
        await _assets.RetireAsync(id, cancellationToken);
        return NoContent();
    }
}
