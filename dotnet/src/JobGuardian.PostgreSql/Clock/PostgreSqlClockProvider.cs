using Dapper;

using JobGuardian.Abstractions.Contracts;
using JobGuardian.PostgreSql.Contracts;

namespace JobGuardian.PostgreSql.Clock;

/// <summary>
/// Provides UTC timestamps from PostgreSQL so JobGuardian uses a database-backed clock.
/// </summary>
public sealed class PostgreSqlClockProvider
    : IClockProvider
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlClockProvider"/> class.
    /// </summary>
    /// <param name="connectionFactory">
    /// Factory used to create database connections for time queries.
    /// </param>
    public PostgreSqlClockProvider(
        IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Gets the current UTC timestamp from PostgreSQL.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The current UTC timestamp reported by PostgreSQL.
    /// </returns>
    public async Task<DateTimeOffset> GetUtcNowAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.CreateAsync(
                cancellationToken);

        return await connection.QuerySingleAsync<DateTimeOffset>(
            """
            SELECT CURRENT_TIMESTAMP
            """);
    }
}