using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Abstractions.Security;
using AssetManagement.Application.Auth;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AssetManagement.UnitTests.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokens = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => $"hashed:{p}");
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((hash, password) => hash == $"hashed:{password}");
        _tokens.Setup(t => t.Generate(It.IsAny<User>()))
            .Returns(new AccessToken("jwt-token", TestData.Now.AddHours(1)));

        _sut = new AuthService(
            _users.Object, _unitOfWork.Object, _hasher.Object, _tokens.Object, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesViewerWithHashedPassword()
    {
        User? added = null;
        _users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(u => added = u);

        var response = await _sut.RegisterAsync(new RegisterRequest
        {
            Email = "  Jane.Doe@Example.COM ",
            FullName = "Jane Doe",
            Password = "Sup3rSecretPass",
        });

        added.Should().NotBeNull();
        added!.Email.Should().Be("jane.doe@example.com");
        added.Role.Should().Be(UserRole.Viewer);
        added.PasswordHash.Should().Be("hashed:Sup3rSecretPass");
        response.AccessToken.Should().Be("jwt-token");
        response.TokenType.Should().Be("Bearer");
        response.User.Role.Should().Be(UserRole.Viewer);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ThrowsConflict()
    {
        _users.Setup(r => r.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _sut.RegisterAsync(new RegisterRequest
        {
            Email = "Jane@example.com",
            FullName = "Jane",
            Password = "Sup3rSecretPass",
        });

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be("user.duplicate_email");
        _users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_CorrectPassword_ReturnsToken()
    {
        var user = new User("tech@example.com", "Tech", UserRole.Technician);
        user.SetPasswordHash("hashed:Correct-Passw0rd");
        _users.Setup(r => r.GetByEmailAsync("tech@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var response = await _sut.LoginAsync(new LoginRequest { Email = "TECH@example.com", Password = "Correct-Passw0rd" });

        response.AccessToken.Should().Be("jwt-token");
        response.User.Role.Should().Be(UserRole.Technician);
    }

    [Theory]
    [InlineData("tech@example.com", "wrong-password")]
    [InlineData("nobody@example.com", "Correct-Passw0rd")]
    public async Task LoginAsync_InvalidCredentials_ThrowsGenericAuthenticationFailure(string email, string password)
    {
        var user = new User("tech@example.com", "Tech", UserRole.Technician);
        user.SetPasswordHash("hashed:Correct-Passw0rd");
        _users.Setup(r => r.GetByEmailAsync("tech@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var act = () => _sut.LoginAsync(new LoginRequest { Email = email, Password = password });

        (await act.Should().ThrowAsync<AuthenticationFailedException>())
            .Which.Message.Should().Be("Invalid email or password.");
        _tokens.Verify(t => t.Generate(It.IsAny<User>()), Times.Never);
    }
}
