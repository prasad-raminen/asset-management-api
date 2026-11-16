using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Assets;

namespace AssetManagement.Application.Abstractions.Persistence;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SerialNumberExistsAsync(string serialNumber, CancellationToken cancellationToken = default);

    Task<PagedResult<Asset>> ListAsync(AssetQueryParameters query, CancellationToken cancellationToken = default);

    void Add(Asset asset);
}
