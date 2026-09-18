using JobGuardian.PostgreSql.Contracts;
using JobGuardian.PostgreSql.Options;

using Microsoft.Extensions.Options;

using Npgsql;

namespace JobGuardian.PostgreSql.Connection;

public sealed class PostgreSqlConnectionFactory
    : IPostgreSqlConnectionFactory
{
    private readonly PostgreSqlOptions _options;

    public PostgreSqlConnectionFactory(
        IOptions<PostgreSqlOptions> options)
    {
        _options = options.Value;
    }

    public async Task<NpgsqlConnection> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(
            _options.ConnectionString);

        await connection.OpenAsync(
            cancellationToken);

        return connection;
    }
}