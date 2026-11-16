using AssetManagement.Domain.Maintenance;

namespace AssetManagement.Application.Maintenance;

public sealed record MaintenanceRecordDto(
    Guid Id,
    Guid AssetId,
    MaintenanceType Type,
    string Description,
    Guid TechnicianId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    decimal? Cost,
    string? ResolutionNotes);

public sealed record StartMaintenanceRequest
{
    public MaintenanceType Type { get; init; }

    public string Description { get; init; } = string.Empty;
}

public sealed record CompleteMaintenanceRequest
{
    public decimal? Cost { get; init; }

    public string? ResolutionNotes { get; init; }
}
