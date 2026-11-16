using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Assets.Policies;

namespace AssetManagement.UnitTests.Domain;

public class MaintenancePolicyTests
{
    private static readonly DateTime Serviced = TestData.Now;

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(45, true)]
    public void CalendarPolicy_IsDueOnceIntervalElapsed(int daysSinceService, bool expected)
    {
        var policy = new CalendarMaintenancePolicy(intervalDays: 30);

        policy.IsMaintenanceDue(new MaintenanceState(Serviced, 0), Serviced.AddDays(daysSinceService))
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public void UsagePolicy_IsDueAfterMaxUses(int uses, bool expected)
    {
        var policy = new UsageMaintenancePolicy(maxUses: 10);

        policy.IsMaintenanceDue(new MaintenanceState(Serviced, uses), Serviced).Should().Be(expected);
    }

    [Fact]
    public void CompositePolicy_IsDueWhenAnyInnerPolicyIsDue()
    {
        var policy = new CompositeMaintenancePolicy(
            new CalendarMaintenancePolicy(90),
            new UsageMaintenancePolicy(20));

        policy.IsMaintenanceDue(new MaintenanceState(Serviced, 0), Serviced.AddDays(1)).Should().BeFalse();
        policy.IsMaintenanceDue(new MaintenanceState(Serviced, 20), Serviced.AddDays(1)).Should().BeTrue();
        policy.IsMaintenanceDue(new MaintenanceState(Serviced, 0), Serviced.AddDays(90)).Should().BeTrue();
        policy.Description.Should().Be("service every 90 days or service every 20 checkouts");
    }

    [Fact]
    public void Policies_RejectNonPositiveThresholds()
    {
        FluentActions.Invoking(() => new CalendarMaintenancePolicy(0)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new UsageMaintenancePolicy(-1)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new CompositeMaintenancePolicy()).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EachAssetType_SuppliesItsOwnPolicy()
    {
        Asset[] assets = [TestData.Scanner(), TestData.Drone(), TestData.Tracker()];

        assets.Select(a => a.MaintenancePolicy.Description).Should().BeEquivalentTo(
            "service every 365 days or service every 50 checkouts",
            "service every 90 days or service every 20 checkouts",
            "service every 180 days");
    }
}
