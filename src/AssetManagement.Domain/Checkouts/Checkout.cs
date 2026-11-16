using AssetManagement.Domain.Common;
using AssetManagement.Domain.Exceptions;

namespace AssetManagement.Domain.Checkouts;

/// <summary>A single loan of an asset to a user. Created only through <see cref="Assets.Asset.CheckOut"/>.</summary>
public sealed class Checkout : Entity
{
    private Checkout()
    {
    }

    internal Checkout(Guid assetId, Guid userId, DateTime checkedOutAtUtc, DateTime dueAtUtc, string? notes)
    {
        AssetId = assetId;
        UserId = userId;
        CheckedOutAtUtc = checkedOutAtUtc;
        DueAtUtc = dueAtUtc;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public Guid AssetId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime CheckedOutAtUtc { get; private set; }

    public DateTime DueAtUtc { get; private set; }

    public DateTime? ReturnedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive => ReturnedAtUtc is null;

    public bool IsOverdue(DateTime utcNow) => IsActive && utcNow > DueAtUtc;

    internal void MarkReturned(DateTime utcNow)
    {
        if (!IsActive)
        {
            throw DomainErrors.CheckoutAlreadyReturned();
        }

        ReturnedAtUtc = utcNow;
    }
}
