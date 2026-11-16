using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

internal sealed class MaintenanceRepository : IMaintenanceRepository
{
    private readonly AppDbContext _db;

    public MaintenanceRepository(AppDbContext db) => _db = db;

    public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.MaintenanceRecords.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceRecord>> ListForAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default) =>
        await _db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.AssetId == assetId)
            .OrderByDescending(m => m.StartedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(MaintenanceRecord record) => _db.MaintenanceRecords.Add(record);
}
