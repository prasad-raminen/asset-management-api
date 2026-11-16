namespace AssetManagement.Application.Abstractions.Persistence;

/// <summary>Commits all changes tracked by the repositories in a single transaction.</summary>
public interface IUnitOfWork
{
    /// <exception cref="Common.Exceptions.ConflictException">
    /// Thrown when another request modified the same data concurrently.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
