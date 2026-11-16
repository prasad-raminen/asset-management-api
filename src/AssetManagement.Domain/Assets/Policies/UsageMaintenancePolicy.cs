namespace AssetManagement.Domain.Assets.Policies;

/// <summary>Maintenance is due after the asset has been checked out a fixed number of times.</summary>
public sealed class UsageMaintenancePolicy : IMaintenancePolicy
{
    public UsageMaintenancePolicy(int maxUses)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxUses);
        MaxUses = maxUses;
    }

    public int MaxUses { get; }

    public string Description => $"service every {MaxUses} checkouts";

    public bool IsMaintenanceDue(MaintenanceState state, DateTime utcNow) =>
        state.UsesSinceLastService >= MaxUses;
}
