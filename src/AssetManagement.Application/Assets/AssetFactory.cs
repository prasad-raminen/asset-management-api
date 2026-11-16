using AssetManagement.Domain.Assets;

namespace AssetManagement.Application.Assets;

/// <summary>Creates the correct <see cref="Asset"/> subtype for a create request.</summary>
public static class AssetFactory
{
    public static Asset Create(CreateAssetRequest request, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Type switch
        {
            AssetType.LaserScanner => new LaserScanner(
                request.Name, request.SerialNumber, request.Manufacturer, request.Model, request.Location,
                Required(request.RangeMeters, nameof(request.RangeMeters)), utcNow),
            AssetType.Drone => new Drone(
                request.Name, request.SerialNumber, request.Manufacturer, request.Model, request.Location,
                Required(request.MaxFlightTimeMinutes, nameof(request.MaxFlightTimeMinutes)), utcNow),
            AssetType.LaserTracker => new LaserTracker(
                request.Name, request.SerialNumber, request.Manufacturer, request.Model, request.Location,
                Required(request.AccuracyMicrometers, nameof(request.AccuracyMicrometers)), utcNow),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Type, "Unsupported asset type."),
        };
    }

    private static T Required<T>(T? value, string name)
        where T : struct =>
        value ?? throw new ArgumentException($"{name} is required for this asset type.", name);
}
