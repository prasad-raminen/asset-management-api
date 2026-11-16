namespace AssetManagement.Application.Auth;

public interface IAuthService
{
    /// <summary>Creates a new account with the <c>Viewer</c> role and returns an access token.</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
