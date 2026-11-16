using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Exceptions;
using AssetManagement.Domain.Maintenance;

namespace AssetManagement.UnitTests.Domain;

public class AssetMaintenanceTests
{
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly DateTime Now = TestData.Now;

    [Fact]
    public void StartMaintenance_AvailableAsset_CreatesOpenRecord()
    {
        var asset = TestData.Tracker();

        var record = asset.StartMaintenance(MaintenanceType.Calibration, "Annual calibration", TechnicianId, Now);

        asset.Status.Should().Be(AssetStatus.UnderMaintenance);
        record.AssetId.Should().Be(asset.Id);
        record.Type.Should().Be(MaintenanceType.Calibration);
        record.TechnicianId.Should().Be(TechnicianId);
        record.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void StartMaintenance_CheckedOutAsset_Throws()
    {
        var asset = TestData.Scanner();
        asset.CheckOut(Guid.NewGuid(), Now.AddDays(1), Now);

        var act = () => asset.StartMaintenance(MaintenanceType.Preventive, "Service", TechnicianId, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.checked_out");
    }

    [Fact]
    public void StartMaintenance_AlreadyUnderMaintenance_Throws()
    {
        var asset = TestData.Scanner();
        asset.StartMaintenance(MaintenanceType.Preventive, "Service", TechnicianId, Now);

        var act = () => asset.StartMaintenance(MaintenanceType.Corrective, "Again", TechnicianId, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.under_maintenance");
    }

    [Fact]
    public void CompleteMaintenance_ResetsServiceCountersAndMakesAvailable()
    {
        var drone = TestData.Drone(registeredAt: Now.AddDays(-100));
        drone.IsMaintenanceDue(Now).Should().BeTrue();
        var record = drone.StartMaintenance(MaintenanceType.Preventive, "Rotor inspection", TechnicianId, Now);
        var completedAt = Now.AddHours(4);

        drone.CompleteMaintenance(record, completedAt, 249.99m, "Replaced rotor set");

        drone.Status.Should().Be(AssetStatus.Available);
        drone.LastServicedAtUtc.Should().Be(completedAt);
        drone.UsesSinceLastService.Should().Be(0);
        drone.IsMaintenanceDue(completedAt).Should().BeFalse();
        record.IsOpen.Should().BeFalse();
        record.Cost.Should().Be(249.99m);
        record.ResolutionNotes.Should().Be("Replaced rotor set");
    }

    [Fact]
    public void CompleteMaintenance_WhenNotUnderMaintenance_Throws()
    {
        var asset = TestData.Scanner();
        var record = asset.StartMaintenance(MaintenanceType.Preventive, "Service", TechnicianId, Now);
        asset.CompleteMaintenance(record, Now, null, null);

        var act = () => asset.CompleteMaintenance(record, Now, null, null);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.not_under_maintenance");
    }

    [Fact]
    public void CompleteMaintenance_RecordOfAnotherAsset_Throws()
    {
        var asset = TestData.Scanner();
        asset.StartMaintenance(MaintenanceType.Preventive, "Service", TechnicianId, Now);
        var foreignRecord = TestData.Drone().StartMaintenance(MaintenanceType.Preventive, "x", TechnicianId, Now);

        var act = () => asset.CompleteMaintenance(foreignRecord, Now, null, null);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("maintenance.mismatch");
    }

    [Fact]
    public void Retire_CheckedOutAsset_Throws()
    {
        var asset = TestData.Scanner();
        asset.CheckOut(Guid.NewGuid(), Now.AddDays(1), Now);

        var act = asset.Retire;

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.checked_out");
    }

    [Fact]
    public void Retire_AssetUnderMaintenance_ThrowsAndKeepsRecordCompletable()
    {
        var asset = TestData.Scanner();
        var record = asset.StartMaintenance(MaintenanceType.Corrective, "Laser fault", TechnicianId, Now);

        var act = asset.Retire;

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.under_maintenance");
        asset.Status.Should().Be(AssetStatus.UnderMaintenance);
        asset.CompleteMaintenance(record, Now.AddHours(2), null, null);
        record.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_RetiredAsset_Throws()
    {
        var asset = TestData.Scanner();
        asset.Retire();

        var act = () => asset.UpdateDetails("New name", "Leica", "RTC360", null);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("asset.retired");
    }
}
