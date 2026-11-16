using AssetManagement.Application.Users;

namespace AssetManagement.Application.Auth;

public sealed record RegisterRequest
{
    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed record LoginRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed record AuthResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, UserDto User);
