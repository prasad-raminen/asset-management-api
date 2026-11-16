using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Assets;
using AssetManagement.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence.Repositories;

internal sealed class AssetRepository : IAssetRepository
{
    private const string LikeEscape = "\\";

    private readonly AppDbContext _db;

    public AssetRepository(AppDbContext db) => _db = db;

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> SerialNumberExistsAsync(string serialNumber, CancellationToken cancellationToken = default) =>
        _db.Assets.AnyAsync(a => a.SerialNumber == serialNumber, cancellationToken);

    public void Add(Asset asset) => _db.Assets.Add(asset);

    public async Task<PagedResult<Asset>> ListAsync(
        AssetQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assets = ApplyFilters(_db.Assets.AsNoTracking(), query);
        var total = await assets.CountAsync(cancellationToken);

        var items = await ApplySorting(assets, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Asset>(items, query.Page, query.PageSize, total);
    }

    private static IQueryable<Asset> ApplyFilters(IQueryable<Asset> assets, AssetQueryParameters query)
    {
        if (query.Status is { } status)
        {
            assets = assets.Where(a => a.Status == status);
        }
        else if (!query.IncludeRetired)
        {
            assets = assets.Where(a => a.Status != AssetStatus.Retired);
        }

        if (query.Type is { } type)
        {
            assets = assets.Where(a => EF.Property<AssetType>(a, AssetConfiguration.Discriminator) == type);
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var pattern = ContainsPattern(query.Location);
            assets = assets.Where(a => a.Location != null && EF.Functions.Like(a.Location, pattern, LikeEscape));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // LIKE is case-insensitive on both SQLite (ASCII) and SQL Server's default collation.
            var pattern = ContainsPattern(query.Search);
            assets = assets.Where(a =>
                EF.Functions.Like(a.Name, pattern, LikeEscape) ||
                EF.Functions.Like(a.SerialNumber, pattern, LikeEscape) ||
                EF.Functions.Like(a.Manufacturer, pattern, LikeEscape) ||
                EF.Functions.Like(a.Model, pattern, LikeEscape));
        }

        return assets;
    }

    private static IQueryable<Asset> ApplySorting(IQueryable<Asset> assets, AssetQueryParameters query)
    {
        var descending = query.SortDirection == SortDirection.Desc;

        IOrderedQueryable<Asset> ordered = (query.SortBy ?? "name").ToUpperInvariant() switch
        {
            "SERIALNUMBER" => Order(assets, a => a.SerialNumber, descending),
            "MANUFACTURER" => Order(assets, a => a.Manufacturer, descending),
            "STATUS" => Order(assets, a => a.Status, descending),
            "TYPE" => Order(assets, a => EF.Property<AssetType>(a, AssetConfiguration.Discriminator), descending),
            "CREATEDAT" => Order(assets, a => a.CreatedAtUtc, descending),
            "LASTSERVICEDAT" => Order(assets, a => a.LastServicedAtUtc, descending),
            _ => Order(assets, a => a.Name, descending),
        };

        // Tie-breaker keeps paging stable when the sort key is not unique.
        return ordered.ThenBy(a => a.Id);
    }

    private static IOrderedQueryable<Asset> Order<TKey>(
        IQueryable<Asset> source,
        System.Linq.Expressions.Expression<Func<Asset, TKey>> key,
        bool descending) =>
        descending ? source.OrderByDescending(key) : source.OrderBy(key);

    private static string ContainsPattern(string term)
    {
        var escaped = term.Trim()
            .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal)
            .Replace("[", LikeEscape + "[", StringComparison.Ordinal);
        return $"%{escaped}%";
    }
}
