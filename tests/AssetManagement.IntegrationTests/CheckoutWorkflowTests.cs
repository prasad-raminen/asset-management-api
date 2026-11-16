using System.Net;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Checkouts;
using AssetManagement.Application.Maintenance;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.Users;
using AssetManagement.IntegrationTests.Infrastructure;
using static AssetManagement.IntegrationTests.Infrastructure.TestClientExtensions;

namespace AssetManagement.IntegrationTests;

public class CheckoutWorkflowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CheckoutWorkflowTests(ApiFactory factory) => _factory = factory;

    private static CheckOutAssetRequest DueIn(TimeSpan span) => new() { DueAtUtc = DateTime.UtcNow.Add(span) };

    [Fact]
    public async Task CheckoutAndReturn_FullLifecycle()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        using var technician = await _factory.CreateClientWithRoleAsync(UserRole.Technician);
        var asset = await admin.CreateAssetAsync(NewScanner());

        var checkout = await technician.PostJsonAsync($"/api/v1/assets/{asset.Id}/checkout", DueIn(TimeSpan.FromDays(2)));
        checkout.StatusCode.Should().Be(HttpStatusCode.Created);
        (await checkout.ReadAsync<CheckoutDto>()).ReturnedAtUtc.Should().BeNull();

        var again = await technician.PostJsonAsync($"/api/v1/assets/{asset.Id}/checkout", DueIn(TimeSpan.FromDays(2)));
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await again.ReadProblemAsync()).Extensions["code"]!.ToString().Should().Be("asset.already_checked_out");

        var current = await (await technician.GetAsync($"/api/v1/assets/{asset.Id}")).ReadAsync<AssetDto>();
        current.Status.Should().Be(AssetStatus.CheckedOut);

        var returned = await technician.PostAsync($"/api/v1/assets/{asset.Id}/return", null);
        returned.StatusCode.Should().Be(HttpStatusCode.OK);
        (await returned.ReadAsync<CheckoutDto>()).ReturnedAtUtc.Should().NotBeNull();

        var returnAgain = await technician.PostAsync($"/api/v1/assets/{asset.Id}/return", null);
        returnAgain.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await returnAgain.ReadProblemAsync()).Extensions["code"]!.ToString().Should().Be("asset.not_checked_out");

        var history = await (await technician.GetAsync($"/api/v1/assets/{asset.Id}/checkouts"))
            .ReadAsync<List<CheckoutDto>>();
        history.Should().ContainSingle().Which.ReturnedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Checkout_AssetUnderMaintenance_IsRejectedUntilMaintenanceCompletes()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var asset = await admin.CreateAssetAsync(NewScanner());

        var start = await admin.PostJsonAsync($"/api/v1/assets/{asset.Id}/maintenance",
            new StartMaintenanceRequest { Type = MaintenanceType.Corrective, Description = "Replace mirror" });
        start.StatusCode.Should().Be(HttpStatusCode.Created);
        var record = await start.ReadAsync<MaintenanceRecordDto>();

        var blocked = await admin.PostJsonAsync($"/api/v1/assets/{asset.Id}/checkout", DueIn(TimeSpan.FromDays(1)));
        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await blocked.ReadProblemAsync()).Extensions["code"]!.ToString().Should().Be("asset.under_maintenance");

        var complete = await admin.PostJsonAsync($"/api/v1/assets/{asset.Id}/maintenance/{record.Id}/complete",
            new CompleteMaintenanceRequest { Cost = 350.50m, ResolutionNotes = "Mirror replaced" });
        complete.StatusCode.Should().Be(HttpStatusCode.OK);
        (await complete.ReadAsync<MaintenanceRecordDto>()).Cost.Should().Be(350.50m);

        (await admin.PostJsonAsync($"/api/v1/assets/{asset.Id}/checkout", DueIn(TimeSpan.FromDays(1))))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var maintenanceHistory = await (await admin.GetAsync($"/api/v1/assets/{asset.Id}/maintenance"))
            .ReadAsync<List<MaintenanceRecordDto>>();
        maintenanceHistory.Should().ContainSingle().Which.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Checkout_DroneBeyondMaximumLoanPeriod_Returns409()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var drone = await admin.CreateAssetAsync(NewDrone());

        var response = await admin.PostJsonAsync($"/api/v1/assets/{drone.Id}/checkout", DueIn(TimeSpan.FromDays(5)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Extensions["code"]!.ToString().Should().Be("checkout.duration_exceeded");
    }

    [Fact]
    public async Task Checkout_DueDateInPast_Returns400()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var asset = await admin.CreateAssetAsync(NewScanner());

        var response = await admin.PostJsonAsync($"/api/v1/assets/{asset.Id}/checkout", DueIn(TimeSpan.FromHours(-1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_UnknownAsset_Returns404()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostJsonAsync($"/api/v1/assets/{Guid.NewGuid()}/checkout", DueIn(TimeSpan.FromDays(1)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
