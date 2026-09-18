using Dapper;

using JobGuardian.Abstractions.Contracts;
using JobGuardian.PostgreSql.Contracts;

namespace JobGuardian.PostgreSql.Clock;

public sealed class PostgreSqlClockProvider
    : IClockProvider
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public PostgreSqlClockProvider(
        IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

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