namespace AssetManagement.Application.Common.Exceptions;

/// <summary>Credentials were invalid. Mapped to HTTP 401 with a deliberately generic message.</summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("Invalid email or password.")
    {
    }
}
