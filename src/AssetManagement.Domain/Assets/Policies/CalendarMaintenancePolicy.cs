namespace AssetManagement.Domain.Assets.Policies;

/// <summary>Maintenance is due once a fixed number of days has elapsed since the last service.</summary>
public sealed class CalendarMaintenancePolicy : IMaintenancePolicy
{
    public CalendarMaintenancePolicy(int intervalDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(intervalDays);
        IntervalDays = intervalDays;
    }

    public int IntervalDays { get; }

    public string Description => $"service every {IntervalDays} days";

    public bool IsMaintenanceDue(MaintenanceState state, DateTime utcNow) =>
        utcNow - state.LastServicedAtUtc >= TimeSpan.FromDays(IntervalDays);
}
