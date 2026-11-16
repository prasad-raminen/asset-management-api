using AssetManagement.Domain.Assets.Policies;

namespace AssetManagement.Domain.Assets;

/// <summary>High-precision portable metrology instrument. Must be recalibrated on a fixed schedule.</summary>
public sealed class LaserTracker : Asset
{
    private static readonly IMaintenancePolicy Policy = new CalendarMaintenancePolicy(intervalDays: 180);

    private LaserTracker()
    {
    }

    public LaserTracker(
        string name,
        string serialNumber,
        string manufacturer,
        string model,
        string? location,
        double accuracyMicrometers,
        DateTime utcNow)
        : base(name, serialNumber, manufacturer, model, location, utcNow)
    {
        SetAccuracy(accuracyMicrometers);
    }

    /// <summary>Rated measurement accuracy in micrometres.</summary>
    public double AccuracyMicrometers { get; private set; }

    public override AssetType Type => AssetType.LaserTracker;

    public override IMaintenancePolicy MaintenancePolicy => Policy;

    public override TimeSpan MaxCheckoutDuration => TimeSpan.FromDays(30);

    public void SetAccuracy(double accuracyMicrometers)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accuracyMicrometers);
        AccuracyMicrometers = accuracyMicrometers;
    }
}
