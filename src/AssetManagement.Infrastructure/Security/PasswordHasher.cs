using AssetManagement.Domain.Users;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = AssetManagement.Application.Abstractions.Security.IPasswordHasher;

namespace AssetManagement.Infrastructure.Security;

/// <summary>
/// Adapts ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2 with HMAC-SHA512,
/// 100k iterations, per-password salt) to the application's hashing abstraction.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private static readonly User HashContext = new("hash-context@localhost", "hash-context");
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return _inner.HashPassword(HashContext, password);
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        var result = _inner.VerifyHashedPassword(HashContext, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
