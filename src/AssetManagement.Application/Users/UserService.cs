using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Users;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository users, IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.ListAsync(cancellationToken);
        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await LoadAsync(id, cancellationToken)).ToDto();

    public async Task<UserDto> ChangeRoleAsync(
        Guid id,
        ChangeRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await LoadAsync(id, cancellationToken);
        var previous = user.Role;
        user.ChangeRole(request.Role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} role changed from {PreviousRole} to {NewRole}", id, previous, user.Role);
        return user.ToDto();
    }

    private async Task<User> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _users.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException("User", id);
}
