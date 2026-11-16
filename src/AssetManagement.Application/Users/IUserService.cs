namespace AssetManagement.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto> ChangeRoleAsync(Guid id, ChangeRoleRequest request, CancellationToken cancellationToken = default);
}
