using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Checkouts;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Checkouts;
using AssetManagement.Domain.Exceptions;
using AssetManagement.Domain.Maintenance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace AssetManagement.UnitTests.Application;

public class CheckoutServiceTests
{
    private readonly Mock<IAssetRepository> _assets = new();
    private readonly Mock<ICheckoutRepository> _checkouts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(TestData.Now);
    private readonly CheckoutService _sut;

    public CheckoutServiceTests()
    {
        _sut = new CheckoutService(
            _assets.Object, _checkouts.Object, _unitOfWork.Object, _clock, NullLogger<CheckoutService>.Instance);
    }

    private Asset GivenAsset(Asset asset)
    {
        _assets.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
        return asset;
    }

    [Fact]
    public async Task CheckOutAsync_AvailableAsset_AddsCheckoutAndSaves()
    {
        var asset = GivenAsset(TestData.Scanner());
        var userId = Guid.NewGuid();
        var due = TestData.Now.AddDays(3);

        var result = await _sut.CheckOutAsync(asset.Id, userId, new CheckOutAssetRequest { DueAtUtc = due });

        result.AssetId.Should().Be(asset.Id);
        result.UserId.Should().Be(userId);
        result.DueAtUtc.Should().Be(due);
        result.IsOverdue.Should().BeFalse();
        asset.Status.Should().Be(AssetStatus.CheckedOut);
        _checkouts.Verify(r => r.Add(It.Is<Checkout>(c => c.AssetId == asset.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckOutAsync_DueDateWithoutOffset_IsTreatedAsUtc()
    {
        var asset = GivenAsset(TestData.Scanner());
        var unspecified = DateTime.SpecifyKind(TestData.Now.AddDays(1), DateTimeKind.Unspecified);

        var result = await _sut.CheckOutAsync(asset.Id, Guid.NewGuid(), new CheckOutAssetRequest { DueAtUtc = unspecified });

        result.DueAtUtc.Should().Be(TestData.Now.AddDays(1));
        result.DueAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task CheckOutAsync_AssetUnderMaintenance_ThrowsAndDoesNotSave()
    {
        var asset = GivenAsset(TestData.Scanner());
        asset.StartMaintenance(MaintenanceType.Corrective, "Repair", Guid.NewGuid(), TestData.Now);

        var act = () => _sut.CheckOutAsync(
            asset.Id, Guid.NewGuid(), new CheckOutAssetRequest { DueAtUtc = TestData.Now.AddDays(1) });

        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("asset.under_maintenance");
        _checkouts.Verify(r => r.Add(It.IsAny<Checkout>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckOutAsync_UnknownAsset_ThrowsNotFound()
    {
        var act = () => _sut.CheckOutAsync(
            Guid.NewGuid(), Guid.NewGuid(), new CheckOutAssetRequest { DueAtUtc = TestData.Now.AddDays(1) });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ReturnAsync_ActiveCheckout_ClosesIt()
    {
        var asset = GivenAsset(TestData.Scanner());
        var checkout = asset.CheckOut(Guid.NewGuid(), TestData.Now.AddDays(2), TestData.Now);
        _checkouts.Setup(r => r.GetActiveForAssetAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(checkout);
        _clock.Advance(TimeSpan.FromDays(1));

        var result = await _sut.ReturnAsync(asset.Id);

        result.ReturnedAtUtc.Should().Be(TestData.Now.AddDays(1));
        asset.Status.Should().Be(AssetStatus.Available);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnAsync_NoActiveCheckout_ThrowsNotCheckedOut()
    {
        var asset = GivenAsset(TestData.Scanner());

        var act = () => _sut.ReturnAsync(asset.Id);

        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("asset.not_checked_out");
    }

    [Fact]
    public async Task GetOverdueAsync_FlagsResultsAsOverdue()
    {
        var asset = TestData.Scanner();
        var checkout = asset.CheckOut(Guid.NewGuid(), TestData.Now.AddDays(1), TestData.Now);
        _clock.Advance(TimeSpan.FromDays(2));
        _checkouts.Setup(r => r.ListOverdueAsync(_clock.GetUtcNow().UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync([checkout]);

        var result = await _sut.GetOverdueAsync();

        result.Should().ContainSingle().Which.IsOverdue.Should().BeTrue();
    }
}
