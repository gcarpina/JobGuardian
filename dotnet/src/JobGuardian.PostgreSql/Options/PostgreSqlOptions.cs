namespace JobGuardian.PostgreSql.Options;

/// <summary>
/// Provides the PostgreSQL connection settings used by JobGuardian persistence components.
/// </summary>
public sealed class PostgreSqlOptions
{
    /// <summary>
    /// Gets the connection string used to connect to PostgreSQL.
    /// </summary>
    public required string ConnectionString { get; init; }
}