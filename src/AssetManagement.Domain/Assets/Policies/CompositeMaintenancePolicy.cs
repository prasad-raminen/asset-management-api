namespace AssetManagement.Domain.Assets.Policies;

/// <summary>Maintenance is due as soon as any of the inner policies says so.</summary>
public sealed class CompositeMaintenancePolicy : IMaintenancePolicy
{
    private readonly IReadOnlyList<IMaintenancePolicy> _policies;

    public CompositeMaintenancePolicy(params IMaintenancePolicy[] policies)
    {
        if (policies.Length == 0)
        {
            throw new ArgumentException("At least one policy is required.", nameof(policies));
        }

        _policies = policies;
    }

    public string Description => string.Join(" or ", _policies.Select(p => p.Description));

    public bool IsMaintenanceDue(MaintenanceState state, DateTime utcNow) =>
        _policies.Any(p => p.IsMaintenanceDue(state, utcNow));
}
