using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Abstractions.Security;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Users;
using AssetManagement.Domain.Users;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = User.NormalizeEmail(request.Email);
        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("user.duplicate_email", "An account with this email already exists.");
        }

        // Self-registered accounts are always read-only; an Admin must promote them.
        var user = new User(email, request.FullName, UserRole.Viewer);
        user.SetPasswordHash(_passwordHasher.Hash(request.Password));
        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered user {UserId}", user.Id);
        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            _logger.LogWarning("Failed login attempt");
            throw new AuthenticationFailedException();
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var token = _tokenGenerator.Generate(user);
        return new AuthResponse(token.Token, "Bearer", token.ExpiresAtUtc, user.ToDto());
    }
}
