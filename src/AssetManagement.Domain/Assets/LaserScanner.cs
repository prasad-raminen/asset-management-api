using AssetManagement.Domain.Assets.Policies;

namespace AssetManagement.Domain.Assets;

/// <summary>Terrestrial 3D laser scanner used for reality capture.</summary>
public sealed class LaserScanner : Asset
{
    private static readonly IMaintenancePolicy Policy = new CompositeMaintenancePolicy(
        new CalendarMaintenancePolicy(intervalDays: 365),
        new UsageMaintenancePolicy(maxUses: 50));

    private LaserScanner()
    {
    }

    public LaserScanner(
        string name,
        string serialNumber,
        string manufacturer,
        string model,
        string? location,
        double rangeMeters,
        DateTime utcNow)
        : base(name, serialNumber, manufacturer, model, location, utcNow)
    {
        SetRange(rangeMeters);
    }

    /// <summary>Maximum measurement range in metres.</summary>
    public double RangeMeters { get; private set; }

    public override AssetType Type => AssetType.LaserScanner;

    public override IMaintenancePolicy MaintenancePolicy => Policy;

    public override TimeSpan MaxCheckoutDuration => TimeSpan.FromDays(14);

    public void SetRange(double rangeMeters)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rangeMeters);
        RangeMeters = rangeMeters;
    }
}
