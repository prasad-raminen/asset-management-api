using AssetManagement.Domain.Users;

namespace AssetManagement.Application.Users;

public sealed record UserDto(Guid Id, string Email, string FullName, UserRole Role, DateTime CreatedAtUtc);

public sealed record ChangeRoleRequest
{
    public UserRole Role { get; init; }
}

internal static class UserMappings
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Email, user.FullName, user.Role, user.CreatedAtUtc);
}
