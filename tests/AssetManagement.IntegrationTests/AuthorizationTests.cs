using System.Net;
using AssetManagement.Application.Checkouts;
using AssetManagement.Domain.Users;
using AssetManagement.IntegrationTests.Infrastructure;
using static AssetManagement.IntegrationTests.Infrastructure.TestClientExtensions;

namespace AssetManagement.IntegrationTests;

/// <summary>Verifies the role matrix: Viewer (read), Technician (operate), Admin (administer).</summary>
public class AuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/v1/assets")]
    [InlineData("/api/v1/checkouts/overdue")]
    [InlineData("/api/v1/users")]
    public async Task ProtectedEndpoints_WithoutToken_Return401(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Viewer_CanRead_ButCannotWrite()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var asset = await admin.CreateAssetAsync(NewScanner());
        using var viewer = await _factory.CreateClientWithRoleAsync(UserRole.Viewer);

        (await viewer.GetAsync("/api/v1/assets")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await viewer.GetAsync($"/api/v1/assets/{asset.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await viewer.PostJsonAsync("/api/v1/assets", NewScanner())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await viewer.DeleteAsync($"/api/v1/assets/{asset.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await viewer.PostJsonAsync($"/api/v1/assets/{asset.Id}/checkout",
                new CheckOutAssetRequest { DueAtUtc = DateTime.UtcNow.AddDays(1) }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Technician_CanOperate_ButCannotAdminister()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var asset = await admin.CreateAssetAsync(NewScanner());
        using var technician = await _factory.CreateClientWithRoleAsync(UserRole.Technician);

        (await technician.PostJsonAsync($"/api/v1/assets/{asset.Id}/checkout",
                new CheckOutAssetRequest { DueAtUtc = DateTime.UtcNow.AddDays(1) }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await technician.GetAsync("/api/v1/checkouts/overdue")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await technician.PostJsonAsync("/api/v1/assets", NewScanner())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await technician.GetAsync("/api/v1/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Forbidden_ResponseIsProblemDetails()
    {
        using var viewer = await _factory.CreateClientWithRoleAsync(UserRole.Viewer);

        var response = await viewer.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Admin_CanListUsers()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
