using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Maintenance;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace AssetManagement.UnitTests.Application;

public class MaintenanceServiceTests
{
    private readonly Mock<IAssetRepository> _assets = new();
    private readonly Mock<IMaintenanceRepository> _records = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(TestData.Now);
    private readonly MaintenanceService _sut;

    public MaintenanceServiceTests()
    {
        _sut = new MaintenanceService(
            _assets.Object, _records.Object, _unitOfWork.Object, _clock, NullLogger<MaintenanceService>.Instance);
    }

    [Fact]
    public async Task StartThenComplete_ReturnsAssetToService()
    {
        var asset = TestData.Tracker();
        var technicianId = Guid.NewGuid();
        _assets.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
        MaintenanceRecord? stored = null;
        _records.Setup(r => r.Add(It.IsAny<MaintenanceRecord>())).Callback<MaintenanceRecord>(r => stored = r);

        var started = await _sut.StartAsync(asset.Id, technicianId, new StartMaintenanceRequest
        {
            Type = MaintenanceType.Calibration,
            Description = "Six-monthly calibration",
        });
        asset.Status.Should().Be(AssetStatus.UnderMaintenance);
        started.TechnicianId.Should().Be(technicianId);

        _records.Setup(r => r.GetByIdAsync(stored!.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _clock.Advance(TimeSpan.FromHours(3));

        var completed = await _sut.CompleteAsync(asset.Id, stored!.Id, new CompleteMaintenanceRequest { Cost = 1200m });

        completed.CompletedAtUtc.Should().Be(TestData.Now.AddHours(3));
        completed.Cost.Should().Be(1200m);
        asset.Status.Should().Be(AssetStatus.Available);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CompleteAsync_RecordBelongsToAnotherAsset_ThrowsNotFound()
    {
        var asset = TestData.Scanner();
        var otherAsset = TestData.Drone();
        var foreignRecord = otherAsset.StartMaintenance(MaintenanceType.Preventive, "x", Guid.NewGuid(), TestData.Now);
        _assets.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
        _records.Setup(r => r.GetByIdAsync(foreignRecord.Id, It.IsAny<CancellationToken>())).ReturnsAsync(foreignRecord);

        var act = () => _sut.CompleteAsync(asset.Id, foreignRecord.Id, new CompleteMaintenanceRequest());

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
