using AssetManagement.Application.Assets;
using AssetManagement.Application.Auth;
using AssetManagement.Application.Checkouts;
using AssetManagement.Domain.Assets;
using Microsoft.Extensions.Time.Testing;

namespace AssetManagement.UnitTests.Validation;

public class ValidatorTests
{
    private static CreateAssetRequest ValidScanner() => new()
    {
        Type = AssetType.LaserScanner,
        Name = "Scanner",
        SerialNumber = "RTC-0001",
        Manufacturer = "Leica Geosystems",
        Model = "RTC360",
        RangeMeters = 130,
    };

    [Fact]
    public void CreateAsset_ValidScanner_Passes()
    {
        new CreateAssetRequestValidator().Validate(ValidScanner()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateAsset_DroneWithoutFlightTime_Fails()
    {
        var request = ValidScanner() with { Type = AssetType.Drone, RangeMeters = null };

        var result = new CreateAssetRequestValidator().Validate(request);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetRequest.MaxFlightTimeMinutes));
    }

    [Fact]
    public void CreateAsset_SpecificationForWrongType_Fails()
    {
        var request = ValidScanner() with { AccuracyMicrometers = 15 };

        var result = new CreateAssetRequestValidator().Validate(request);

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetRequest.AccuracyMicrometers));
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    [InlineData("")]
    public void CreateAsset_InvalidSerialNumber_Fails(string serial)
    {
        var result = new CreateAssetRequestValidator().Validate(ValidScanner() with { SerialNumber = serial });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetRequest.SerialNumber));
    }

    [Theory]
    [InlineData(0, 20, null)]
    [InlineData(1, 101, null)]
    [InlineData(1, 20, "purchasePrice")]
    public void AssetQuery_InvalidPagingOrSort_Fails(int page, int pageSize, string? sortBy)
    {
        var query = new AssetQueryParameters { Page = page, PageSize = pageSize, SortBy = sortBy };

        new AssetQueryParametersValidator().Validate(query).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("serialNumber")]
    [InlineData("CREATEDAT")]
    public void AssetQuery_WhitelistedSortField_Passes(string sortBy)
    {
        var query = new AssetQueryParameters { SortBy = sortBy };

        new AssetQueryParametersValidator().Validate(query).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase123")]
    [InlineData("ALLUPPERCASE123")]
    [InlineData("NoDigitsInThisOne")]
    public void Register_WeakPassword_Fails(string password)
    {
        var request = new RegisterRequest { Email = "a@example.com", FullName = "A", Password = password };

        new RegisterRequestValidator().Validate(request).Errors
            .Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void CheckOut_DueDateInPast_Fails()
    {
        var clock = new FakeTimeProvider(TestData.Now);
        var validator = new CheckOutAssetRequestValidator(clock);

        validator.Validate(new CheckOutAssetRequest { DueAtUtc = TestData.Now.AddMinutes(-1) }).IsValid.Should().BeFalse();
        validator.Validate(new CheckOutAssetRequest { DueAtUtc = TestData.Now.AddDays(1) }).IsValid.Should().BeTrue();
    }
}
