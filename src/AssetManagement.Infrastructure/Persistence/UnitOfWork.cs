using AssetManagement.Application.Abstractions.Persistence;
using AssetManagement.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db) => _db = db;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException(
                "concurrency_conflict",
                "The resource was modified by another request. Reload it and try again.",
                ex);
        }
    }
}
