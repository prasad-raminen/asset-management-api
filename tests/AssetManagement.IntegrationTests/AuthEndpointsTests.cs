using System.Net;
using System.Net.Http.Headers;
using AssetManagement.Application.Auth;
using AssetManagement.Application.Users;
using AssetManagement.Domain.Users;
using AssetManagement.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using static AssetManagement.IntegrationTests.Infrastructure.TestClientExtensions;

namespace AssetManagement.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<ApiFactory>
{
    private const string Password = "Valid-Passw0rd-123";
    private readonly ApiFactory _factory;

    public AuthEndpointsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ValidRequest_Returns201WithViewerToken()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();

        var response = await client.PostJsonAsync("/api/v1/auth/register",
            new RegisterRequest { Email = email, FullName = "New User", Password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().EndWith("/api/v1/auth/me");
        var body = await response.ReadAsync<AuthResponse>();
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.TokenType.Should().Be("Bearer");
        body.User.Email.Should().Be(email);
        body.User.Role.Should().Be(UserRole.Viewer);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Problem()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest { Email = UniqueEmail(), FullName = "Dup", Password = Password };
        (await client.PostJsonAsync("/api/v1/auth/register", request)).StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostJsonAsync("/api/v1/auth/register", request with { Email = request.Email.ToUpperInvariant() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.ReadProblemAsync();
        problem.Extensions["code"]!.ToString().Should().Be("user.duplicate_email");
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400ValidationProblem()
    {
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync("/api/v1/auth/register",
            new RegisterRequest { Email = "not-an-email", FullName = "", Password = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        problem.Errors.Keys.Should().Contain(["email", "fullName", "password"]);
    }

    [Fact]
    public async Task Login_ThenMe_ReturnsCurrentUser()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        await client.PostJsonAsync("/api/v1/auth/register",
            new RegisterRequest { Email = email, FullName = "Login User", Password = Password });

        var auth = await client.LoginAsync(email.ToUpperInvariant(), Password);
        client.WithToken(auth.AccessToken);
        var me = await client.GetAsync("/api/v1/auth/me");

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await me.ReadAsync<UserDto>()).Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401Problem()
    {
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = ApiFactory.AdminEmail, Password = "Wrong-Passw0rd-123" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.ReadProblemAsync()).Detail.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        var client = _factory.CreateClient();
        var auth = await client.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        var parts = auth.AccessToken.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}.{new string('A', parts[2].Length)}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
