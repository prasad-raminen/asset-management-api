using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Common;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Checkouts;
using AssetManagement.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Application.Checkouts;

public sealed class CheckoutService : ICheckoutService
{
    private readonly IAssetRepository _assets;
    private readonly ICheckoutRepository _checkouts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        IAssetRepository assets,
        ICheckoutRepository checkouts,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        ILogger<CheckoutService> logger)
    {
        _assets = assets;
        _checkouts = checkouts;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    private DateTime UtcNow => _clock.GetUtcNow().UtcDateTime;

    public async Task<CheckoutDto> CheckOutAsync(
        Guid assetId,
        Guid userId,
        CheckOutAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await LoadAssetAsync(assetId, cancellationToken);
        var now = UtcNow;

        // Business rules (availability, maintenance, loan period) live in the domain model.
        var checkout = asset.CheckOut(userId, request.DueAtUtc.AsUtc(), now, request.Notes);
        _checkouts.Add(checkout);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Asset {AssetId} checked out to user {UserId} until {DueAtUtc}",
            asset.Id, userId, checkout.DueAtUtc);

        return ToDto(checkout, now);
    }

    public async Task<CheckoutDto> ReturnAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await LoadAssetAsync(assetId, cancellationToken);
        var checkout = await _checkouts.GetActiveForAssetAsync(assetId, cancellationToken)
            ?? throw DomainErrors.AssetNotCheckedOut(asset.SerialNumber);

        var now = UtcNow;
        var wasOverdue = checkout.IsOverdue(now);
        asset.Return(checkout, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (wasOverdue)
        {
            _logger.LogWarning(
                "Asset {AssetId} returned late by user {UserId} (due {DueAtUtc})",
                asset.Id, checkout.UserId, checkout.DueAtUtc);
        }
        else
        {
            _logger.LogInformation("Asset {AssetId} returned by user {UserId}", asset.Id, checkout.UserId);
        }

        return ToDto(checkout, now);
    }

    public async Task<IReadOnlyList<CheckoutDto>> GetHistoryAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        await LoadAssetAsync(assetId, cancellationToken);
        var history = await _checkouts.ListForAssetAsync(assetId, cancellationToken);
        var now = UtcNow;
        return history.Select(c => ToDto(c, now)).ToList();
    }

    public async Task<IReadOnlyList<CheckoutDto>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = UtcNow;
        var overdue = await _checkouts.ListOverdueAsync(now, cancellationToken);
        return overdue.Select(c => ToDto(c, now)).ToList();
    }

    private async Task<Asset> LoadAssetAsync(Guid assetId, CancellationToken cancellationToken) =>
        await _assets.GetByIdAsync(assetId, cancellationToken)
        ?? throw new NotFoundException("Asset", assetId);

    private static CheckoutDto ToDto(Checkout checkout, DateTime utcNow) => new(
        checkout.Id,
        checkout.AssetId,
        checkout.UserId,
        checkout.CheckedOutAtUtc,
        checkout.DueAtUtc,
        checkout.ReturnedAtUtc,
        checkout.Notes,
        checkout.IsOverdue(utcNow));
}
