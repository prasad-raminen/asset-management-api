using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Assets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace AssetManagement.UnitTests.Application;

public class AssetServiceTests
{
    private readonly Mock<IAssetRepository> _assets = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(TestData.Now);
    private readonly AssetService _sut;

    public AssetServiceTests()
    {
        _sut = new AssetService(_assets.Object, _unitOfWork.Object, _clock, NullLogger<AssetService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_NewSerialNumber_PersistsCorrectSubtype()
    {
        var request = new CreateAssetRequest
        {
            Type = AssetType.Drone,
            Name = "Drone Charlie",
            SerialNumber = "  UAV-0003 ",
            Manufacturer = "DJI",
            Model = "Matrice 350 RTK",
            MaxFlightTimeMinutes = 55,
        };
        Asset? added = null;
        _assets.Setup(r => r.Add(It.IsAny<Asset>())).Callback<Asset>(a => added = a);

        var result = await _sut.CreateAsync(request);

        added.Should().BeOfType<Drone>();
        added!.SerialNumber.Should().Be("UAV-0003");
        result.Type.Should().Be(AssetType.Drone);
        result.Status.Should().Be(AssetStatus.Available);
        result.Specifications.MaxFlightTimeMinutes.Should().Be(55);
        result.MaxCheckoutDays.Should().Be(3);
        _assets.Verify(r => r.SerialNumberExistsAsync("UAV-0003", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSerialNumber_ThrowsConflictWithoutSaving()
    {
        _assets.Setup(r => r.SerialNumberExistsAsync("RTC-0001", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new CreateAssetRequest
        {
            Type = AssetType.LaserScanner,
            Name = "Scanner",
            SerialNumber = "RTC-0001",
            Manufacturer = "Leica",
            Model = "RTC360",
            RangeMeters = 130,
        };

        var act = () => _sut.CreateAsync(request);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be("asset.duplicate_serial_number");
        _assets.Verify(r => r.Add(It.IsAny<Asset>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ThrowsNotFound()
    {
        _assets.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Asset?)null);

        var act = () => _sut.GetAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAsync_ReportsMaintenanceDueUsingCurrentTime()
    {
        var tracker = TestData.Tracker();
        _assets.Setup(r => r.GetByIdAsync(tracker.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tracker);

        (await _sut.GetAsync(tracker.Id)).Maintenance.IsDue.Should().BeFalse();

        _clock.Advance(TimeSpan.FromDays(181));
        (await _sut.GetAsync(tracker.Id)).Maintenance.IsDue.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_AppliesDetailsAndTypeSpecificSpecifications()
    {
        var scanner = TestData.Scanner();
        _assets.Setup(r => r.GetByIdAsync(scanner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(scanner);

        var result = await _sut.UpdateAsync(scanner.Id, new UpdateAssetRequest
        {
            Name = "RTC360 Renamed",
            Manufacturer = "Leica Geosystems",
            Model = "RTC360 LT",
            Location = "Field Office",
            RangeMeters = 120,
        });

        result.Name.Should().Be("RTC360 Renamed");
        result.Location.Should().Be("Field Office");
        result.Specifications.RangeMeters.Should().Be(120);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetireAsync_SetsStatusRetired()
    {
        var scanner = TestData.Scanner();
        _assets.Setup(r => r.GetByIdAsync(scanner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(scanner);

        await _sut.RetireAsync(scanner.Id);

        scanner.Status.Should().Be(AssetStatus.Retired);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
