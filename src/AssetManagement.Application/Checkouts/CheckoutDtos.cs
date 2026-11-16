namespace AssetManagement.Application.Checkouts;

public sealed record CheckoutDto(
    Guid Id,
    Guid AssetId,
    Guid UserId,
    DateTime CheckedOutAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    string? Notes,
    bool IsOverdue);

public sealed record CheckOutAssetRequest
{
    public DateTime DueAtUtc { get; init; }

    public string? Notes { get; init; }
}
