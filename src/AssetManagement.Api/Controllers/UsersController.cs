using AssetManagement.Api.Authorization;
using AssetManagement.Application.Users;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers;

/// <summary>User administration (Admin only).</summary>
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Policy = Policies.CanAdminister)]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Lists all users.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken) =>
        Ok(await _users.ListAsync(cancellationToken));

    /// <summary>Changes a user's role (Viewer, Technician or Admin).</summary>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> ChangeRole(
        Guid id,
        ChangeRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _users.ChangeRoleAsync(id, request, cancellationToken));
}
