using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Exceptions;
using AssetManagement.Domain.Maintenance;

namespace AssetManagement.UnitTests.Domain;

public class AssetCheckoutTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = TestData.Now;

    [Fact]
    public void CheckOut_AvailableAsset_MarksCheckedOutAndCreatesCheckout()
    {
        var asset = TestData.Scanner();
        var due = Now.AddDays(5);

        var checkout = asset.CheckOut(UserId, due, Now, "  site survey  ");

        asset.Status.Should().Be(AssetStatus.CheckedOut);
        asset.UsesSinceLastService.Should().Be(1);
        checkout.AssetId.Should().Be(asset.Id);
        checkout.UserId.Should().Be(UserId);
        checkout.CheckedOutAtUtc.Should().Be(Now);
        checkout.DueAtUtc.Should().Be(due);
        checkout.Notes.Should().Be("site survey");
        checkout.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CheckOut_AlreadyCheckedOut_Throws()
    {
        var asset = TestData.Scanner();
        asset.CheckOut(UserId, Now.AddDays(1), Now);

        var act = () => asset.CheckOut(Guid.NewGuid(), Now.AddDays(1), Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.already_checked_out");
    }

    [Fact]
    public void CheckOut_UnderMaintenance_Throws()
    {
        var asset = TestData.Scanner();
        asset.StartMaintenance(MaintenanceType.Corrective, "Cracked lens", UserId, Now);

        var act = () => asset.CheckOut(UserId, Now.AddDays(1), Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.under_maintenance");
    }

    [Fact]
    public void CheckOut_Retired_Throws()
    {
        var asset = TestData.Scanner();
        asset.Retire();

        var act = () => asset.CheckOut(UserId, Now.AddDays(1), Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.retired");
    }

    [Fact]
    public void CheckOut_DueDateNotInFuture_Throws()
    {
        var asset = TestData.Scanner();

        var act = () => asset.CheckOut(UserId, Now, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("checkout.due_date_in_past");
        asset.Status.Should().Be(AssetStatus.Available);
    }

    [Theory]
    [InlineData(AssetType.Drone, 3)]
    [InlineData(AssetType.LaserScanner, 14)]
    [InlineData(AssetType.LaserTracker, 30)]
    public void CheckOut_EnforcesTypeSpecificMaximumLoanPeriod(AssetType type, int maxDays)
    {
        Asset Create() => type switch
        {
            AssetType.Drone => TestData.Drone(),
            AssetType.LaserScanner => TestData.Scanner(),
            _ => TestData.Tracker(),
        };

        var withinLimit = Create();
        withinLimit.CheckOut(UserId, Now.AddDays(maxDays), Now);
        withinLimit.Status.Should().Be(AssetStatus.CheckedOut);

        var overLimit = Create();
        var act = () => overLimit.CheckOut(UserId, Now.AddDays(maxDays).AddMinutes(1), Now);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("checkout.duration_exceeded");
    }

    [Fact]
    public void CheckOut_LaserTrackerPastCalibrationInterval_Throws()
    {
        var tracker = TestData.Tracker(registeredAt: Now.AddDays(-180));

        var act = () => tracker.CheckOut(UserId, Now.AddDays(1), Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.maintenance_overdue");
    }

    [Fact]
    public void CheckOut_DroneAfterTwentyFlights_RequiresMaintenance()
    {
        var drone = TestData.Drone();
        for (var i = 0; i < 20; i++)
        {
            var checkout = drone.CheckOut(UserId, Now.AddDays(1), Now);
            drone.Return(checkout, Now);
        }

        drone.IsMaintenanceDue(Now).Should().BeTrue();
        var act = () => drone.CheckOut(UserId, Now.AddDays(1), Now);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.maintenance_overdue");
    }

    [Fact]
    public void Return_ActiveCheckout_MakesAssetAvailable()
    {
        var asset = TestData.Scanner();
        var checkout = asset.CheckOut(UserId, Now.AddDays(2), Now);
        var returnedAt = Now.AddDays(1);

        asset.Return(checkout, returnedAt);

        asset.Status.Should().Be(AssetStatus.Available);
        checkout.ReturnedAtUtc.Should().Be(returnedAt);
        checkout.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Return_NotCheckedOut_Throws()
    {
        var asset = TestData.Scanner();
        var other = TestData.Drone();
        var checkout = other.CheckOut(UserId, Now.AddDays(1), Now);

        var act = () => asset.Return(checkout, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.not_checked_out");
    }

    [Fact]
    public void Return_CheckoutOfAnotherAsset_Throws()
    {
        var asset = TestData.Scanner();
        asset.CheckOut(UserId, Now.AddDays(1), Now);
        var foreignCheckout = TestData.Drone().CheckOut(UserId, Now.AddDays(1), Now);

        var act = () => asset.Return(foreignCheckout, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("checkout.mismatch");
    }

    [Fact]
    public void Checkout_IsOverdue_OnlyWhileActiveAndPastDue()
    {
        var asset = TestData.Scanner();
        var checkout = asset.CheckOut(UserId, Now.AddDays(1), Now);

        checkout.IsOverdue(Now.AddHours(23)).Should().BeFalse();
        checkout.IsOverdue(Now.AddDays(2)).Should().BeTrue();

        asset.Return(checkout, Now.AddDays(2));
        checkout.IsOverdue(Now.AddDays(3)).Should().BeFalse();
    }
}
