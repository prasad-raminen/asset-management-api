using AssetManagement.Api.Authorization;
using AssetManagement.Api.Extensions;
using AssetManagement.Application.Checkouts;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers;

/// <summary>The checkout / return workflow.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class CheckoutsController : ControllerBase
{
    private readonly ICheckoutService _checkouts;

    public CheckoutsController(ICheckoutService checkouts) => _checkouts = checkouts;

    /// <summary>Checks an asset out to the current user.</summary>
    /// <remarks>
    /// Fails with 409 if the asset is already checked out, under maintenance, retired, overdue for
    /// maintenance, or if the due date exceeds the asset type's maximum loan period.
    /// </remarks>
    [HttpPost("assets/{assetId:guid}/checkout")]
    [Authorize(Policy = Policies.CanOperate)]
    [ProducesResponseType<CheckoutDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckoutDto>> CheckOut(
        Guid assetId,
        CheckOutAssetRequest request,
        CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.CheckOutAsync(assetId, User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(History), new { assetId, version = "1.0" }, checkout);
    }

    /// <summary>Returns a checked-out asset.</summary>
    [HttpPost("assets/{assetId:guid}/return")]
    [Authorize(Policy = Policies.CanOperate)]
    [ProducesResponseType<CheckoutDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckoutDto>> Return(Guid assetId, CancellationToken cancellationToken) =>
        Ok(await _checkouts.ReturnAsync(assetId, cancellationToken));

    /// <summary>Lists the checkout history of an asset, newest first.</summary>
    [HttpGet("assets/{assetId:guid}/checkouts")]
    [Authorize(Policy = Policies.CanView)]
    [ProducesResponseType<IReadOnlyList<CheckoutDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CheckoutDto>>> History(
        Guid assetId,
        CancellationToken cancellationToken) =>
        Ok(await _checkouts.GetHistoryAsync(assetId, cancellationToken));

    /// <summary>Lists all open checkouts that are past their due date.</summary>
    [HttpGet("checkouts/overdue")]
    [Authorize(Policy = Policies.CanOperate)]
    [ProducesResponseType<IReadOnlyList<CheckoutDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CheckoutDto>>> Overdue(CancellationToken cancellationToken) =>
        Ok(await _checkouts.GetOverdueAsync(cancellationToken));
}
