using AssetManagement.Domain.Maintenance;

namespace AssetManagement.Application.Abstractions.Persistence;

public interface IMaintenanceRepository
{
    Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceRecord>> ListForAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    void Add(MaintenanceRecord record);
}
