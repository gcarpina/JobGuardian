using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Models;
using JobGuardian.Core.State;
using JobGuardian.PostgreSql.AuditStore;
using JobGuardian.PostgreSql.DependencyInjection;
using JobGuardian.PostgreSql.LeaseStore;
using JobGuardian.PostgreSql.State;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var connectionString =
    Environment.GetEnvironmentVariable(
        "JOBGUARDIAN_POSTGRESQL_CONNECTION_STRING");

services
    .AddJobGuardian()
    .UsePostgreSql(
        string.IsNullOrWhiteSpace(connectionString)
            ? "Host=localhost;Database=jobguardian;Username=jobguardian;Password=not-used"
            : connectionString);

services.AddJob<SampleJob>(
    new JobKey("consumer-check", "demo", "sample"),
    new JobExecutionPolicy
    {
        LeaseDuration = TimeSpan.FromMinutes(1),
        HeartbeatInterval = TimeSpan.FromSeconds(10),
        FailurePolicy = FailurePolicy.Ignore
    });

using var provider = services.BuildServiceProvider();
var leaseStore = provider.GetRequiredService<ILeaseStore>();
var jobStateRepository = provider.GetRequiredService<IJobStateRepository>();
var jobStateManager = provider.GetRequiredService<IJobStateManager>();
var auditStore = provider.GetRequiredService<IAuditStore>();
var job = provider.GetRequiredService<SampleJob>();

if (leaseStore is not PostgreSqlLeaseStore)
{
    throw new InvalidOperationException(
        "The PostgreSQL package did not register its lease store.");
}

if (jobStateRepository is not PostgreSqlJobStateRepository)
{
    throw new InvalidOperationException(
        "The PostgreSQL package did not register its job state repository.");
}

if (auditStore is not PostgreSqlAuditStore)
{
    throw new InvalidOperationException(
        "The PostgreSQL package did not register its audit store.");
}

Console.WriteLine(
    $"Resolved {leaseStore.GetType().Name}, {jobStateRepository.GetType().Name}, and {auditStore.GetType().Name}.");
Console.WriteLine(
    $"Resolved registered job {job.GetType().Name}.");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    await VerifyPostgreSqlAsync(
        leaseStore,
        jobStateRepository,
        jobStateManager);
}

static async Task VerifyPostgreSqlAsync(
    ILeaseStore leaseStore,
    IJobStateRepository jobStateRepository,
    IJobStateManager jobStateManager)
{
    var jobKey =
        new JobKey(
            "consumer-check",
            "demo",
            "postgresql-package-flow");

    var execution =
        new ActiveExecution
        {
            JobKey = jobKey,
            ExecutionId = Guid.NewGuid(),
            OwnerId = $"consumer-check-{Guid.NewGuid():N}"
        };

    if (!await leaseStore.TryAcquireAsync(
            execution,
            TimeSpan.FromSeconds(30)))
    {
        throw new InvalidOperationException(
            "The PostgreSQL package could not acquire a lease.");
    }

    var activeLease =
        await leaseStore.GetActiveLeaseAsync(
            jobKey);

    if (activeLease is null ||
        activeLease.ExecutionId != execution.ExecutionId)
    {
        throw new InvalidOperationException(
            "The PostgreSQL package did not return the acquired lease.");
    }

    if (!await leaseStore.RenewAsync(
            jobKey,
            execution.ExecutionId,
            TimeSpan.FromSeconds(30)))
    {
        throw new InvalidOperationException(
            "The PostgreSQL package could not renew the lease.");
    }

    if (!await leaseStore.ReleaseAsync(
            jobKey,
            execution.ExecutionId))
    {
        throw new InvalidOperationException(
            "The PostgreSQL package could not release the lease.");
    }

    await jobStateRepository.SetAsync(
        jobKey,
        JobState.Blocked);

    var state =
        await jobStateRepository.GetAsync(
            jobKey);

    if (state != JobState.Blocked)
    {
        throw new InvalidOperationException(
            "The PostgreSQL package did not persist the blocked job state.");
    }

    await jobStateManager.ResetAsync(
        jobKey,
        "package-consumer-sample");

    state =
        await jobStateRepository.GetAsync(
            jobKey);

    if (state is not null)
    {
        throw new InvalidOperationException(
            "The PostgreSQL package did not reset the blocked job state.");
    }

    Console.WriteLine(
        "Verified PostgreSQL lease operations, state persistence and audited reset.");
}

internal sealed class SampleJob : IJob
{
    public Task ExecuteAsync(
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
