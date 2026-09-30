using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Models;
using JobGuardian.PostgreSql.DependencyInjection;
using JobGuardian.PostgreSql.LeaseStore;
using JobGuardian.PostgreSql.State;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services
    .AddJobGuardian()
    .UsePostgreSql("Host=localhost;Database=jobguardian;Username=jobguardian;Password=not-used");

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

Console.WriteLine(
    $"Resolved {leaseStore.GetType().Name} and {jobStateRepository.GetType().Name}.");
Console.WriteLine(
    $"Resolved registered job {job.GetType().Name}.");

internal sealed class SampleJob : IJob
{
    public Task ExecuteAsync(
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
