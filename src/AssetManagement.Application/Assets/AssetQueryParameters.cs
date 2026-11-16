using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Assets;

namespace AssetManagement.Application.Assets;

/// <summary>Query-string parameters for <c>GET /api/v1/assets</c>: paging, filtering and sorting.</summary>
public sealed class AssetQueryParameters
{
    public const int MaxPageSize = 100;

    public static readonly IReadOnlyCollection<string> SortableFields =
        ["name", "serialNumber", "manufacturer", "status", "type", "createdAt", "lastServicedAt"];

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Free-text match on name, serial number, manufacturer or model.</summary>
    public string? Search { get; init; }

    public AssetStatus? Status { get; init; }

    public AssetType? Type { get; init; }

    public string? Location { get; init; }

    /// <summary>Retired assets are hidden unless explicitly requested (or filtered by <see cref="Status"/>).</summary>
    public bool IncludeRetired { get; init; }

    /// <summary>One of <see cref="SortableFields"/> (case-insensitive). Defaults to <c>name</c>.</summary>
    public string? SortBy { get; init; }

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;
}
