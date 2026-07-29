using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using CustomerService.Application.Abstractions.Persistence;

using Microsoft.Extensions.Configuration;

namespace CustomerService.Infrastructure.Data.Persistence.Context;
public sealed class ApplicationDbConnectionFactory : IApplicationDbConnectionFactory
{
    private readonly string _connectionString;
    public ApplicationDbConnectionFactory(string connectionString) => _connectionString = connectionString;
    public ApplicationDbConnectionFactory(IConfiguration configuration) => _connectionString = configuration.GetConnectionString(DatabaseConstants.DefaultConnection) ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    public DbConnection CreateConnection() => new SqlConnection(_connectionString);
    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}