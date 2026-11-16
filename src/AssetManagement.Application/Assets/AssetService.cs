using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Assets;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Application.Assets;

public sealed class AssetService : IAssetService
{
    private readonly IAssetRepository _assets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        IAssetRepository assets,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        ILogger<AssetService> logger)
    {
        _assets = assets;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    private DateTime UtcNow => _clock.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<AssetDto>> ListAsync(
        AssetQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var page = await _assets.ListAsync(query, cancellationToken);
        var now = UtcNow;
        return page.Map(asset => asset.ToDto(now));
    }

    public async Task<AssetDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await LoadAsync(id, cancellationToken);
        return asset.ToDto(UtcNow);
    }

    public async Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serialNumber = request.SerialNumber.Trim();
        if (await _assets.SerialNumberExistsAsync(serialNumber, cancellationToken))
        {
            throw new ConflictException(
                "asset.duplicate_serial_number",
                $"An asset with serial number '{serialNumber}' already exists.");
        }

        var asset = AssetFactory.Create(request with { SerialNumber = serialNumber }, UtcNow);
        _assets.Add(asset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Registered {AssetType} {AssetId} with serial number {SerialNumber}",
            asset.Type, asset.Id, asset.SerialNumber);

        return asset.ToDto(UtcNow);
    }

    public async Task<AssetDto> UpdateAsync(
        Guid id,
        UpdateAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await LoadAsync(id, cancellationToken);
        asset.UpdateDetails(request.Name, request.Manufacturer, request.Model, request.Location);
        asset.ApplySpecifications(request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated asset {AssetId}", asset.Id);
        return asset.ToDto(UtcNow);
    }

    public async Task RetireAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await LoadAsync(id, cancellationToken);
        asset.Retire();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Retired asset {AssetId}", asset.Id);
    }

    private async Task<Asset> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _assets.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException("Asset", id);
}
