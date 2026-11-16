using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Auth;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.IntegrationTests.Infrastructure;

internal static class TestClientExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@tests.local";

    public static string UniqueSerial(string prefix = "SN") => $"{prefix}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value ?? throw new InvalidOperationException($"Response body was not a {typeof(T).Name}.");
    }

    public static Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response) =>
        response.ReadAsync<ProblemDetails>();

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PutAsJsonAsync(url, body, Json);

    public static async Task<AuthResponse> LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostJsonAsync("/api/v1/auth/login", new LoginRequest { Email = email, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.ReadAsync<AuthResponse>();
    }

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Creates a client authenticated as the seeded administrator.</summary>
    public static async Task<HttpClient> CreateAdminClientAsync(this ApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await client.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        return client.WithToken(auth.AccessToken);
    }

    /// <summary>
    /// Registers a new account (always a Viewer), promotes it through the admin API when another role is
    /// requested, then logs in again so the token carries the new role claim.
    /// </summary>
    public static async Task<HttpClient> CreateClientWithRoleAsync(this ApiFactory factory, UserRole role)
    {
        var email = UniqueEmail(role.ToString().ToLowerInvariant());
        const string password = "Test-Passw0rd-123";

        var client = factory.CreateClient();
        var register = await client.PostJsonAsync("/api/v1/auth/register",
            new RegisterRequest { Email = email, FullName = $"{role} User", Password = password });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var registered = await register.ReadAsync<AuthResponse>();

        if (role != UserRole.Viewer)
        {
            using var admin = await factory.CreateAdminClientAsync();
            var promote = await admin.PutJsonAsync($"/api/v1/users/{registered.User.Id}/role", new { role });
            promote.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var auth = await client.LoginAsync(email, password);
        auth.User.Role.Should().Be(role);
        return client.WithToken(auth.AccessToken);
    }

    public static CreateAssetRequest NewScanner(string? serial = null, string name = "Test Scanner") => new()
    {
        Type = AssetType.LaserScanner,
        Name = name,
        SerialNumber = serial ?? UniqueSerial("RTC"),
        Manufacturer = "Leica Geosystems",
        Model = "RTC360",
        Location = "Integration Lab",
        RangeMeters = 130,
    };

    public static CreateAssetRequest NewDrone(string? serial = null, string name = "Test Drone") => new()
    {
        Type = AssetType.Drone,
        Name = name,
        SerialNumber = serial ?? UniqueSerial("UAV"),
        Manufacturer = "DJI",
        Model = "Matrice 350 RTK",
        Location = "Integration Lab",
        MaxFlightTimeMinutes = 55,
    };

    public static async Task<AssetDto> CreateAssetAsync(this HttpClient adminClient, CreateAssetRequest request)
    {
        var response = await adminClient.PostJsonAsync("/api/v1/assets", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.ReadAsync<AssetDto>();
    }
}
