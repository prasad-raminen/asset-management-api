using AssetManagement.Domain.Assets.Policies;

namespace AssetManagement.Domain.Assets;

/// <summary>Survey drone (UAV). Wear is driven by flights, so it is serviced by usage as well as by date.</summary>
public sealed class Drone : Asset
{
    private static readonly IMaintenancePolicy Policy = new CompositeMaintenancePolicy(
        new CalendarMaintenancePolicy(intervalDays: 90),
        new UsageMaintenancePolicy(maxUses: 20));

    private Drone()
    {
    }

    public Drone(
        string name,
        string serialNumber,
        string manufacturer,
        string model,
        string? location,
        int maxFlightTimeMinutes,
        DateTime utcNow)
        : base(name, serialNumber, manufacturer, model, location, utcNow)
    {
        SetMaxFlightTime(maxFlightTimeMinutes);
    }

    public int MaxFlightTimeMinutes { get; private set; }

    public override AssetType Type => AssetType.Drone;

    public override IMaintenancePolicy MaintenancePolicy => Policy;

    /// <summary>Drones are short-loan items because of battery and airworthiness checks.</summary>
    public override TimeSpan MaxCheckoutDuration => TimeSpan.FromDays(3);

    public void SetMaxFlightTime(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minutes);
        MaxFlightTimeMinutes = minutes;
    }
}
