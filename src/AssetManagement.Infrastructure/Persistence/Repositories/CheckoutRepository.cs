using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Domain.Checkouts;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

internal sealed class CheckoutRepository : ICheckoutRepository
{
    private readonly AppDbContext _db;

    public CheckoutRepository(AppDbContext db) => _db = db;

    public Task<Checkout?> GetActiveForAssetAsync(Guid assetId, CancellationToken cancellationToken = default) =>
        _db.Checkouts.FirstOrDefaultAsync(c => c.AssetId == assetId && c.ReturnedAtUtc == null, cancellationToken);

    public async Task<IReadOnlyList<Checkout>> ListForAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default) =>
        await _db.Checkouts.AsNoTracking()
            .Where(c => c.AssetId == assetId)
            .OrderByDescending(c => c.CheckedOutAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Checkout>> ListOverdueAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        await _db.Checkouts.AsNoTracking()
            .Where(c => c.ReturnedAtUtc == null && c.DueAtUtc < utcNow)
            .OrderBy(c => c.DueAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(Checkout checkout) => _db.Checkouts.Add(checkout);
}
