namespace AssetManagement.Domain.Common;

/// <summary>
/// Base type for every persisted aggregate/entity. Provides identity and audit timestamps.
/// </summary>
public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; protected set; }

    public DateTime CreatedAtUtc { get; protected set; }

    public DateTime? UpdatedAtUtc { get; protected set; }

    /// <summary>Called by the persistence layer when the entity is first added.</summary>
    public void MarkCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    /// <summary>Called by the persistence layer whenever the entity is modified.</summary>
    public void MarkUpdated(DateTime utcNow) => UpdatedAtUtc = utcNow;
}
