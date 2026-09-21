using Npgsql;

namespace JobGuardian.PostgreSql.Contracts;

/// <summary>
/// Creates database connections for PostgreSQL-backed JobGuardian persistence.
/// </summary>
public interface IPostgreSqlConnectionFactory
{
    /// <summary>
    /// Creates an open PostgreSQL connection.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// An open database connection for PostgreSQL persistence operations.
    /// </returns>
    Task<NpgsqlConnection> CreateAsync(
        CancellationToken cancellationToken = default);
}