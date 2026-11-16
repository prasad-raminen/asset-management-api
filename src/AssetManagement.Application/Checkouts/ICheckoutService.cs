namespace AssetManagement.Application.Checkouts;

public interface ICheckoutService
{
    Task<CheckoutDto> CheckOutAsync(
        Guid assetId,
        Guid userId,
        CheckOutAssetRequest request,
        CancellationToken cancellationToken = default);

    Task<CheckoutDto> ReturnAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CheckoutDto>> GetHistoryAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CheckoutDto>> GetOverdueAsync(CancellationToken cancellationToken = default);
}
