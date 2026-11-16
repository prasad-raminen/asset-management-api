using System.Net;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Assets;
using AssetManagement.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using static AssetManagement.IntegrationTests.Infrastructure.TestClientExtensions;

namespace AssetManagement.IntegrationTests;

public class AssetEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AssetEndpointsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_ThenGet_RoundTripsAsset()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var request = NewDrone();

        var response = await admin.PostJsonAsync("/api/v1/assets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var created = await response.ReadAsync<AssetDto>();
        created.Type.Should().Be(AssetType.Drone);
        created.Status.Should().Be(AssetStatus.Available);
        created.Specifications.MaxFlightTimeMinutes.Should().Be(55);
        created.MaxCheckoutDays.Should().Be(3);

        var fetched = await (await admin.GetAsync(response.Headers.Location)).ReadAsync<AssetDto>();
        fetched.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Create_DuplicateSerialNumber_Returns409()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var request = NewScanner();
        await admin.CreateAssetAsync(request);

        var response = await admin.PostJsonAsync("/api/v1/assets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Extensions["code"]!.ToString().Should().Be("asset.duplicate_serial_number");
    }

    [Fact]
    public async Task Create_DroneWithoutFlightTime_Returns400()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostJsonAsync("/api/v1/assets", NewDrone() with { MaxFlightTimeMinutes = null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ValidationProblemDetails>()).Errors.Should().ContainKey("maxFlightTimeMinutes");
    }

    [Fact]
    public async Task Get_UnknownId_Returns404Problem()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync($"/api/v1/assets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadProblemAsync()).Title.Should().Be("Resource not found");
    }

    [Fact]
    public async Task List_SupportsFilteringSortingAndPaging()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var tag = Guid.NewGuid().ToString("N")[..8];
        await admin.CreateAssetAsync(NewDrone(name: $"{tag} Drone A"));
        await admin.CreateAssetAsync(NewDrone(name: $"{tag} Drone B"));
        await admin.CreateAssetAsync(NewDrone(name: $"{tag} Drone C"));
        await admin.CreateAssetAsync(NewScanner(name: $"{tag} Scanner"));

        var response = await admin.GetAsync(
            $"/api/v1/assets?search={tag.ToUpperInvariant()}&type=Drone&sortBy=name&sortDirection=Desc&page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.ReadAsync<PagedResult<AssetDto>>();
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(2);
        page.HasNextPage.Should().BeTrue();
        page.Items.Select(a => a.Name).Should().Equal($"{tag} Drone C", $"{tag} Drone B");

        var second = await (await admin.GetAsync(
            $"/api/v1/assets?search={tag}&type=Drone&sortBy=name&sortDirection=Desc&page=2&pageSize=2"))
            .ReadAsync<PagedResult<AssetDto>>();
        second.Items.Select(a => a.Name).Should().Equal($"{tag} Drone A");
    }

    [Theory]
    [InlineData("sortBy=password")]
    [InlineData("pageSize=1000")]
    [InlineData("page=0")]
    public async Task List_InvalidQuery_Returns400(string query)
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync($"/api/v1/assets?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ChangesDetails()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var asset = await admin.CreateAssetAsync(NewScanner());

        var response = await admin.PutJsonAsync($"/api/v1/assets/{asset.Id}", new UpdateAssetRequest
        {
            Name = "Renamed Scanner",
            Manufacturer = asset.Manufacturer,
            Model = asset.Model,
            Location = "Field Office",
            RangeMeters = 150,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadAsync<AssetDto>();
        updated.Name.Should().Be("Renamed Scanner");
        updated.Location.Should().Be("Field Office");
        updated.Specifications.RangeMeters.Should().Be(150);
        updated.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_RetiresAssetAndHidesItByDefault()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var asset = await admin.CreateAssetAsync(NewScanner());

        (await admin.DeleteAsync($"/api/v1/assets/{asset.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var visible = await (await admin.GetAsync($"/api/v1/assets?search={asset.SerialNumber}"))
            .ReadAsync<PagedResult<AssetDto>>();
        visible.TotalCount.Should().Be(0);

        var withRetired = await (await admin.GetAsync($"/api/v1/assets?search={asset.SerialNumber}&includeRetired=true"))
            .ReadAsync<PagedResult<AssetDto>>();
        withRetired.Items.Should().ContainSingle().Which.Status.Should().Be(AssetStatus.Retired);
    }
}
