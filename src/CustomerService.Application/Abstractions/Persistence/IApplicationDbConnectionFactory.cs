using System.Data.Common;

namespace CustomerService.Application.Abstractions.Persistence;
/// <summary>
/// Creates database connections for Dapper based reads and writes.
/// </summary>
public interface IApplicationDbConnectionFactory
{
    /// <summary>
    /// Creates a closed database connection.
    /// </summary>
    DbConnection CreateConnection();
    /// <summary>
    /// Creates and opens a database connection asynchronously.
    /// </summary>
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}