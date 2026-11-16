namespace AssetManagement.Domain.Assets.Policies;

/// <summary>
/// Strategy that decides when a piece of equipment must be serviced before it can be used again.
/// Each asset type supplies its own policy, so the checkout rule is polymorphic.
/// </summary>
public interface IMaintenancePolicy
{
    /// <summary>Human-readable description, e.g. "every 180 days".</summary>
    string Description { get; }

    bool IsMaintenanceDue(MaintenanceState state, DateTime utcNow);
}

/// <summary>The subset of asset state a maintenance policy needs to make its decision.</summary>
public readonly record struct MaintenanceState(DateTime LastServicedAtUtc, int UsesSinceLastService);
