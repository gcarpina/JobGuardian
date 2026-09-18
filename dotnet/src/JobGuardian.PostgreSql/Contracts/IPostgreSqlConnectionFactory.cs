using Npgsql;

namespace JobGuardian.PostgreSql.Contracts;

public interface IPostgreSqlConnectionFactory
{
    Task<NpgsqlConnection> CreateAsync(
        CancellationToken cancellationToken = default);
}