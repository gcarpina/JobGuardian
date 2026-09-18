namespace JobGuardian.PostgreSql.IntegrationTests.Infrastructure;

[CollectionDefinition("postgresql")]
public sealed class PostgreSqlCollection
    : ICollectionFixture<PostgreSqlFixture>
{
}