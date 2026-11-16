using AssetManagement.Application.Common.Models;

namespace AssetManagement.Application.Assets;

public interface IAssetService
{
    Task<PagedResult<AssetDto>> ListAsync(AssetQueryParameters query, CancellationToken cancellationToken = default);

    Task<AssetDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);

    Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default);

    Task RetireAsync(Guid id, CancellationToken cancellationToken = default);
}
