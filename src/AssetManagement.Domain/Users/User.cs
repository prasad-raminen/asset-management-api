using AssetManagement.Domain.Common;

namespace AssetManagement.Domain.Users;

public sealed class User : Entity
{
    private User()
    {
        Email = string.Empty;
        FullName = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string email, string fullName, UserRole role = UserRole.Viewer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        Email = NormalizeEmail(email);
        FullName = fullName.Trim();
        Role = role;
        PasswordHash = string.Empty;
    }

    /// <summary>Stored lower-cased and trimmed so lookups are case-insensitive on every provider.</summary>
    public string Email { get; private set; }

    public string FullName { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void ChangeRole(UserRole role) => Role = role;
}
