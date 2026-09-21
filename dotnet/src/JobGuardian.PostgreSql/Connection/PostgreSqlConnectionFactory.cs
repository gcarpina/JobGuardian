using JobGuardian.PostgreSql.Contracts;
using JobGuardian.PostgreSql.Options;

using Microsoft.Extensions.Options;

using Npgsql;

namespace JobGuardian.PostgreSql.Connection;

/// <summary>
/// Creates and opens PostgreSQL connections using the configured connection string.
/// </summary>
public sealed class PostgreSqlConnectionFactory
    : IPostgreSqlConnectionFactory
{
    private readonly PostgreSqlOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConnectionFactory"/> class.
    /// </summary>
    /// <param name="options">
    /// The configured PostgreSQL options.
    /// </param>
    public PostgreSqlConnectionFactory(
        IOptions<PostgreSqlOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Creates and opens a PostgreSQL connection.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation.
    /// </param>
    /// <returns>
    /// An open <see cref="NpgsqlConnection"/> instance.
    /// </returns>
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