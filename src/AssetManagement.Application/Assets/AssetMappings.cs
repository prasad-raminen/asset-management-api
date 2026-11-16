using AssetManagement.Domain.Assets;

namespace AssetManagement.Application.Assets;

internal static class AssetMappings
{
    public static AssetDto ToDto(this Asset asset, DateTime utcNow) => new(
        asset.Id,
        asset.Type,
        asset.Name,
        asset.SerialNumber,
        asset.Manufacturer,
        asset.Model,
        asset.Location,
        asset.Status,
        asset.ToSpecifications(),
        new MaintenanceInfoDto(
            asset.MaintenancePolicy.Description,
            asset.LastServicedAtUtc,
            asset.UsesSinceLastService,
            asset.IsMaintenanceDue(utcNow)),
        (int)asset.MaxCheckoutDuration.TotalDays,
        asset.CreatedAtUtc,
        asset.UpdatedAtUtc);

    private static AssetSpecificationsDto ToSpecifications(this Asset asset) => asset switch
    {
        LaserScanner scanner => new AssetSpecificationsDto(RangeMeters: scanner.RangeMeters),
        Drone drone => new AssetSpecificationsDto(MaxFlightTimeMinutes: drone.MaxFlightTimeMinutes),
        LaserTracker tracker => new AssetSpecificationsDto(AccuracyMicrometers: tracker.AccuracyMicrometers),
        _ => new AssetSpecificationsDto(),
    };

    /// <summary>Applies whichever type-specific specification fields are present in the request.</summary>
    public static void ApplySpecifications(this Asset asset, UpdateAssetRequest request)
    {
        switch (asset)
        {
            case LaserScanner scanner when request.RangeMeters is { } range:
                scanner.SetRange(range);
                break;
            case Drone drone when request.MaxFlightTimeMinutes is { } minutes:
                drone.SetMaxFlightTime(minutes);
                break;
            case LaserTracker tracker when request.AccuracyMicrometers is { } accuracy:
                tracker.SetAccuracy(accuracy);
                break;
        }
    }
}
