using AssetManagement.Domain.Assets;

namespace AssetManagement.Application.Assets;

public sealed record AssetDto(
    Guid Id,
    AssetType Type,
    string Name,
    string SerialNumber,
    string Manufacturer,
    string Model,
    string? Location,
    AssetStatus Status,
    AssetSpecificationsDto Specifications,
    MaintenanceInfoDto Maintenance,
    int MaxCheckoutDays,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

/// <summary>Type-specific technical data. Only the field relevant to the asset's type is populated.</summary>
public sealed record AssetSpecificationsDto(
    double? RangeMeters = null,
    int? MaxFlightTimeMinutes = null,
    double? AccuracyMicrometers = null);

public sealed record MaintenanceInfoDto(
    string Policy,
    DateTime LastServicedAtUtc,
    int UsesSinceLastService,
    bool IsDue);

public sealed record CreateAssetRequest
{
    public AssetType Type { get; init; }

    public string Name { get; init; } = string.Empty;

    public string SerialNumber { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public string? Location { get; init; }

    /// <summary>Required when <see cref="Type"/> is <see cref="AssetType.LaserScanner"/>.</summary>
    public double? RangeMeters { get; init; }

    /// <summary>Required when <see cref="Type"/> is <see cref="AssetType.Drone"/>.</summary>
    public int? MaxFlightTimeMinutes { get; init; }

    /// <summary>Required when <see cref="Type"/> is <see cref="AssetType.LaserTracker"/>.</summary>
    public double? AccuracyMicrometers { get; init; }
}

public sealed record UpdateAssetRequest
{
    public string Name { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public string? Location { get; init; }

    /// <summary>Optional; applies to laser scanners only.</summary>
    public double? RangeMeters { get; init; }

    /// <summary>Optional; applies to drones only.</summary>
    public int? MaxFlightTimeMinutes { get; init; }

    /// <summary>Optional; applies to laser trackers only.</summary>
    public double? AccuracyMicrometers { get; init; }
}
