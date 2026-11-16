using AssetManagement.Domain.Checkouts;

namespace AssetManagement.Application.Abstractions.Persistence;

public interface ICheckoutRepository
{
    Task<Checkout?> GetActiveForAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Checkout>> ListForAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Checkout>> ListOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    void Add(Checkout checkout);
}
