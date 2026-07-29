using Microsoft.EntityFrameworkCore;
using CustomerService.Domain.Entities;

namespace CustomerService.Application.Abstractions.Persistence;
/// <summary>
/// Represents the application persistence context contract.
/// </summary>
public interface IApplicationDbContext
{    

    /// <summary>
    /// Returns a DbSet for the given entity type.
    /// </summary>
    DbSet<TEntity> Set<TEntity>()
        where TEntity : class;
    /// <summary>
    /// Persists pending changes asynchronously.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}