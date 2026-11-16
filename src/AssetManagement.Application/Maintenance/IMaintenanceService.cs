namespace AssetManagement.Application.Maintenance;

public interface IMaintenanceService
{
    Task<MaintenanceRecordDto> StartAsync(
        Guid assetId,
        Guid technicianId,
        StartMaintenanceRequest request,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRecordDto> CompleteAsync(
        Guid assetId,
        Guid recordId,
        CompleteMaintenanceRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceRecordDto>> GetHistoryAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);
}
