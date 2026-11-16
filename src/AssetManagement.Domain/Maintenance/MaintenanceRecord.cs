using AssetManagement.Domain.Common;
using AssetManagement.Domain.Exceptions;

namespace AssetManagement.Domain.Maintenance;

/// <summary>A servicing job on an asset. Created only through <see cref="Assets.Asset.StartMaintenance"/>.</summary>
public sealed class MaintenanceRecord : Entity
{
    private MaintenanceRecord()
    {
        Description = string.Empty;
    }

    internal MaintenanceRecord(
        Guid assetId,
        MaintenanceType type,
        string description,
        Guid technicianId,
        DateTime startedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        AssetId = assetId;
        Type = type;
        Description = description.Trim();
        TechnicianId = technicianId;
        StartedAtUtc = startedAtUtc;
    }

    public Guid AssetId { get; private set; }

    public MaintenanceType Type { get; private set; }

    public string Description { get; private set; }

    public Guid TechnicianId { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public decimal? Cost { get; private set; }

    public string? ResolutionNotes { get; private set; }

    public bool IsOpen => CompletedAtUtc is null;

    internal void Complete(DateTime utcNow, decimal? cost, string? resolutionNotes)
    {
        if (!IsOpen)
        {
            throw DomainErrors.MaintenanceAlreadyCompleted();
        }

        if (cost is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Cost cannot be negative.");
        }

        CompletedAtUtc = utcNow;
        Cost = cost;
        ResolutionNotes = string.IsNullOrWhiteSpace(resolutionNotes) ? null : resolutionNotes.Trim();
    }
}
