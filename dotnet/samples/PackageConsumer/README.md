# Package consumer sample

This console application demonstrates restoring and using the NuGet package API for JobGuardian.
It targets .NET 8 and .NET 10 and uses the package version declared in
`dotnet/Directory.Build.props`. It verifies that the PostgreSQL provider and a registered job can
be resolved from dependency injection.

The packages are not required to be published to NuGet.org to run this sample. Build them into a
local feed from the repository root:

```sh
dotnet pack dotnet/JobGuardian.sln --configuration Release --output ./artifacts/packages
dotnet restore dotnet/samples/PackageConsumer/JobGuardian.PackageConsumer.csproj \
  --source ./artifacts/packages \
  --source https://api.nuget.org/v3/index.json \
  --packages ./artifacts/nuget-cache
dotnet run --project dotnet/samples/PackageConsumer/JobGuardian.PackageConsumer.csproj \
  --configuration Release --no-restore
```

Without `JOBGUARDIAN_POSTGRESQL_CONNECTION_STRING`, the sample validates package restore,
compilation, transitive dependencies, and dependency-injection registrations without connecting to
PostgreSQL. When that environment variable is set, it also verifies lease acquisition, lookup,
renewal, release, and persisted job state against the configured database. This writes a lease and
job-state data, so use a disposable database. CI runs this flow against a disposable PostgreSQL
service after applying the schema restored from the package. To run the database checks locally,
apply the schema as described below, set the variable to an Npgsql connection string, then run:

```sh
dotnet run --project dotnet/samples/PackageConsumer/JobGuardian.PackageConsumer.csproj \
  --configuration Release --no-restore
```

## Applying the PostgreSQL schema

NuGet restores the schema into its package cache; it does not copy the script into the consumer
project or apply it to a database. In this sample, the `--packages` option above puts the cache
under `artifacts/nuget-cache`, so the schema path is:

```text
artifacts/nuget-cache/jobguardian.postgresql/<package-version>/contentFiles/any/any/sql/postgresql/V001_initial_schema.sql
```

To apply it to a new database from the repository root, set `DATABASE_URL` to the intended
PostgreSQL connection string and locate the restored script by package version:

```sh
SCHEMA_PATH=$(find artifacts/nuget-cache/jobguardian.postgresql \
  -type f \
  -path '*/contentFiles/any/any/sql/postgresql/V001_initial_schema.sql' \
  -print -quit)
test -n "$SCHEMA_PATH"
psql "$DATABASE_URL" \
  -f "$SCHEMA_PATH"
```

For a normal consumer restore, NuGet uses the global packages folder. Find its location with
`dotnet nuget locals global-packages --list`; append
`jobguardian.postgresql/<package-version>/contentFiles/any/any/sql/postgresql/V001_initial_schema.sql`
to that folder. The default on Linux and macOS is `~/.nuget/packages`; on Windows it is
`%USERPROFILE%\.nuget\packages`. A custom `NUGET_PACKAGES` setting or `globalPackagesFolder`
configuration changes this location.

Applying the schema changes the target database. Review the SQL and confirm the target before
running `psql`; JobGuardian does not apply it automatically.
