using Npgsql;

using Testcontainers.PostgreSql;

namespace JobGuardian.PostgreSql.IntegrationTests.Infrastructure;

public sealed class PostgreSqlFixture
    : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17")
            .Build();

    public string ConnectionString
        => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await ApplySchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private async Task ApplySchemaAsync()
    {
        var schemaPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../../sql/postgresql/V001_initial_schema.sql"));

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException(
                $"Schema file not found: {schemaPath}");
        }

        var sql =
            await File.ReadAllTextAsync(
                schemaPath);

        await using var connection =
            new NpgsqlConnection(
                ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new NpgsqlCommand(
                sql,
                connection);

        await command.ExecuteNonQueryAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection =
            new NpgsqlConnection(
                ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new NpgsqlCommand(
                """
                TRUNCATE TABLE
                    jobguardian_active_executions,
                    jobguardian_job_state,
                    jobguardian_execution_history;
                """,
                connection);

        await command.ExecuteNonQueryAsync();
    }
}