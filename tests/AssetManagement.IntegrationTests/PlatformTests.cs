using System.Net;
using AssetManagement.Application.Auth;
using AssetManagement.IntegrationTests.Infrastructure;

namespace AssetManagement.IntegrationTests;

public class PlatformTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PlatformTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_IsAnonymousAndHealthy()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task UnsupportedApiVersion_Returns404()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/v2/assets");

        // With URL-segment versioning an unknown version matches no route.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Responses_ReportSupportedApiVersions()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/v1/assets");

        response.Headers.GetValues("api-supported-versions").Should().Contain("1.0");
    }
}

/// <summary>Uses a separate host with a tiny login quota to observe the auth rate-limit policy.</summary>
public class RateLimitingTests : IClassFixture<RateLimitingTests.StrictAuthLimitFactory>
{
    private readonly StrictAuthLimitFactory _factory;

    public RateLimitingTests(StrictAuthLimitFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ExceedingAuthQuota_Returns429WithRetryAfter()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest { Email = "nobody@tests.local", Password = "Wrong-Passw0rd-1" };

        for (var i = 0; i < StrictAuthLimitFactory.Limit; i++)
        {
            (await client.PostJsonAsync("/api/v1/auth/login", request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var throttled = await client.PostJsonAsync("/api/v1/auth/login", request);

        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        throttled.Headers.RetryAfter.Should().NotBeNull();
        (await throttled.ReadProblemAsync()).Title.Should().Be("Too many requests");
    }

    public sealed class StrictAuthLimitFactory : ApiFactory
    {
        public const int Limit = 3;

        protected override int AuthPermitLimit => Limit;
    }
}
