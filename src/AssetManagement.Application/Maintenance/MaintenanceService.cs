using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Application.Maintenance;

public sealed class MaintenanceService : IMaintenanceService
{
    private readonly IAssetRepository _assets;
    private readonly IMaintenanceRepository _records;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        IAssetRepository assets,
        IMaintenanceRepository records,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        ILogger<MaintenanceService> logger)
    {
        _assets = assets;
        _records = records;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    private DateTime UtcNow => _clock.GetUtcNow().UtcDateTime;

    public async Task<MaintenanceRecordDto> StartAsync(
        Guid assetId,
        Guid technicianId,
        StartMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await LoadAssetAsync(assetId, cancellationToken);
        var record = asset.StartMaintenance(request.Type, request.Description, technicianId, UtcNow);
        _records.Add(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{MaintenanceType} maintenance {RecordId} started on asset {AssetId} by {TechnicianId}",
            record.Type, record.Id, asset.Id, technicianId);

        return ToDto(record);
    }

    public async Task<MaintenanceRecordDto> CompleteAsync(
        Guid assetId,
        Guid recordId,
        CompleteMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await LoadAssetAsync(assetId, cancellationToken);
        var record = await _records.GetByIdAsync(recordId, cancellationToken);
        if (record is null || record.AssetId != assetId)
        {
            throw new NotFoundException("Maintenance record", recordId);
        }

        asset.CompleteMaintenance(record, UtcNow, request.Cost, request.ResolutionNotes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Maintenance {RecordId} completed on asset {AssetId}", record.Id, asset.Id);
        return ToDto(record);
    }

    public async Task<IReadOnlyList<MaintenanceRecordDto>> GetHistoryAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        await LoadAssetAsync(assetId, cancellationToken);
        var records = await _records.ListForAssetAsync(assetId, cancellationToken);
        return records.Select(ToDto).ToList();
    }

    private async Task<Asset> LoadAssetAsync(Guid assetId, CancellationToken cancellationToken) =>
        await _assets.GetByIdAsync(assetId, cancellationToken)
        ?? throw new NotFoundException("Asset", assetId);

    private static MaintenanceRecordDto ToDto(MaintenanceRecord record) => new(
        record.Id,
        record.AssetId,
        record.Type,
        record.Description,
        record.TechnicianId,
        record.StartedAtUtc,
        record.CompletedAtUtc,
        record.Cost,
        record.ResolutionNotes);
}
