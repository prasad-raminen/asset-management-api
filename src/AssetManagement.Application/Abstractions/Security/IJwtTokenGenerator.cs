using AssetManagement.Domain.Users;

namespace AssetManagement.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
