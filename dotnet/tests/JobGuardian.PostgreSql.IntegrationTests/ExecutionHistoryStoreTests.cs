using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.DependencyInjection;
using JobGuardian.Core.Models;
using JobGuardian.PostgreSql.DependencyInjection;
using JobGuardian.PostgreSql.IntegrationTests.Infrastructure;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace JobGuardian.PostgreSql.IntegrationTests;

[Collection("postgresql")]
public sealed class ExecutionHistoryStoreTests
    : PostgreSqlTestBase
{
    public ExecutionHistoryStoreTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task CT1100_Create_And_Update_Should_Persist_Execution_History()
    {
        await Fixture.ResetDatabaseAsync();

        var store =
            CreateExecutionHistoryStore();

        var executionId =
            Guid.NewGuid();

        var startedAt =
            DateTimeOffset.FromUnixTimeMilliseconds(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var entry =
            new ExecutionHistoryEntry
            {
                ExecutionId = executionId,
                JobKey = new JobKey(
                    TenantId,
                    JobNamespace,
                    JobName),
                CorrelationId = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(),
                ProtocolVersion = "1.0",
                SchemaVersion = "1.0",
                ApplicationName = "JobGuardian.IntegrationTests",
                ApplicationVersion = "1.2.3",
                Environment = "Test",
                ClusterName = "test-cluster",
                OwnerId = "test-node",
                StartedAtUtc = startedAt,
                RunType = RunType.Scheduled,
                TriggeredBy = TriggeredBy.Scheduler,
                ExecutionMetadataJson = """{"attempt":1}"""
            };

        await store.CreateAsync(entry);

        var startedEntry =
            await store.GetAsync(executionId);

        Assert.NotNull(startedEntry);
        Assert.Equal(
            entry with
            {
                ExecutionMetadataJson =
                    startedEntry.ExecutionMetadataJson
            },
            startedEntry);

        using var metadata =
            JsonDocument.Parse(
                startedEntry.ExecutionMetadataJson!);

        Assert.Equal(
            1,
            metadata.RootElement
                .GetProperty("attempt")
                .GetInt32());

        var completedEntry =
            entry with
            {
                EndedAtUtc = startedAt.AddSeconds(12),
                Outcome = ExecutionOutcome.Failed,
                FailureCategory = "ApplicationError"
            };

        await store.UpdateAsync(completedEntry);

        var actual =
            await store.GetAsync(executionId);

        Assert.NotNull(actual);
        Assert.Equal(
            completedEntry with
            {
                ExecutionMetadataJson =
                    actual.ExecutionMetadataJson
            },
            actual);
    }

    [Fact]
    public async Task CT1110_Get_Missing_Execution_Should_Return_Null()
    {
        await Fixture.ResetDatabaseAsync();

        var store =
            CreateExecutionHistoryStore();

        var entry =
            await store.GetAsync(
                Guid.NewGuid());

        Assert.Null(entry);
    }

    [Fact]
    public async Task CT030_Query_Should_Filter_By_Tenant_Job_Time_And_Outcome_With_Pagination()
    {
        await Fixture.ResetDatabaseAsync();

        var store =
            CreateExecutionHistoryStore();
        var baseTime =
            DateTimeOffset.UtcNow.AddMinutes(-10);
        var entries =
            new[]
            {
                CreateHistoryEntry(Guid.NewGuid(), TenantId, baseTime, ExecutionOutcome.Succeeded),
                CreateHistoryEntry(Guid.NewGuid(), TenantId, baseTime.AddMinutes(1), ExecutionOutcome.Failed),
                CreateHistoryEntry(Guid.NewGuid(), TenantId, baseTime.AddMinutes(2), ExecutionOutcome.Succeeded),
                CreateHistoryEntry(Guid.NewGuid(), "other-tenant", baseTime.AddMinutes(3), ExecutionOutcome.Succeeded)
            };

        foreach (var entry in entries)
        {
            await store.CreateAsync(entry);
            await store.UpdateAsync(entry);
        }

        var page =
            await store.QueryAsync(
                new ExecutionHistoryQuery
                {
                    JobKey = new JobKey(TenantId, JobNamespace, JobName),
                    StartedFromUtc = baseTime,
                    StartedBeforeUtc = baseTime.AddMinutes(3),
                    Outcome = ExecutionOutcome.Succeeded,
                    Page = 1,
                    PageSize = 1
                });

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(entries[2].ExecutionId, page.Items[0].ExecutionId);
        Assert.Equal(1, page.Page);
        Assert.Equal(1, page.PageSize);
    }

    [Fact]
    public async Task CT038_Recovery_Should_Abandon_Only_Previously_Acquired_Incomplete_History()
    {
        await Fixture.ResetDatabaseAsync();

        var store =
            CreateExecutionHistoryStore();
        var acquiredIncomplete =
            CreateHistoryEntry(
                Guid.NewGuid(),
                TenantId,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                null);
        var neverAcquired =
            CreateHistoryEntry(
                Guid.NewGuid(),
                TenantId,
                DateTimeOffset.UtcNow.AddMinutes(-4),
                null);
        var current =
            CreateHistoryEntry(
                Guid.NewGuid(),
                TenantId,
                DateTimeOffset.UtcNow,
                null);

        await store.CreateAsync(acquiredIncomplete);
        await store.CreateAsync(neverAcquired);
        await store.CreateAsync(current);
        await store.MarkLeaseAcquiredAsync(acquiredIncomplete.ExecutionId);
        await store.MarkLeaseAcquiredAsync(current.ExecutionId);

        var recovered =
            await store.MarkPreviousIncompleteAsAbandonedAsync(
                current.JobKey,
                current.ExecutionId,
                DateTimeOffset.UtcNow);

        var recoveredEntry =
            await store.GetAsync(acquiredIncomplete.ExecutionId);
        var untouchedEntry =
            await store.GetAsync(neverAcquired.ExecutionId);
        var currentEntry =
            await store.GetAsync(current.ExecutionId);

        Assert.Equal(1, recovered);
        Assert.Equal(ExecutionOutcome.Abandoned, recoveredEntry?.Outcome);
        Assert.Equal(
            ExecutionOutcomeReason.LeaseExpiredWithoutOutcome,
            recoveredEntry?.OutcomeReason);
        Assert.Null(untouchedEntry?.Outcome);
        Assert.Null(currentEntry?.Outcome);
    }

    [Fact]
    public async Task CT032_Query_Should_Reject_Invalid_Pagination()
    {
        var store =
            CreateExecutionHistoryStore();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => store.QueryAsync(
                new ExecutionHistoryQuery
                {
                    JobKey = new JobKey(TenantId, JobNamespace, JobName),
                    StartedFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
                    StartedBeforeUtc = DateTimeOffset.UtcNow,
                    PageSize = 101
                }));
    }

    [Fact]
    public async Task CT1120_Update_Missing_Execution_Should_Throw()
    {
        await Fixture.ResetDatabaseAsync();

        var store =
            CreateExecutionHistoryStore();

        var entry =
            new ExecutionHistoryEntry
            {
                ExecutionId = Guid.NewGuid(),
                JobKey = new JobKey(
                    TenantId,
                    JobNamespace,
                    JobName),
                ProtocolVersion = "1.0",
                SchemaVersion = "1.0",
                ApplicationName = "JobGuardian.IntegrationTests",
                Environment = "Test",
                OwnerId = "test-node",
                StartedAtUtc = DateTimeOffset.UtcNow,
                RunType = RunType.Scheduled,
                TriggeredBy = TriggeredBy.Scheduler,
                EndedAtUtc = DateTimeOffset.UtcNow,
                Outcome = ExecutionOutcome.Succeeded
            };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => store.UpdateAsync(entry));
    }

    [Fact]
    public async Task CT1130_Hosted_Runtime_Should_Persist_Completed_Execution()
    {
        await Fixture.ResetDatabaseAsync();

        var signal =
            new ExecutionSignal();
        var correlationId =
            Guid.NewGuid();

        var jobKey =
            new JobKey(
                TenantId,
                JobNamespace,
                "history-integration");

        using var host =
            Host.CreateDefaultBuilder()
                .ConfigureServices(
                    services =>
                    {
                        services.AddSingleton(signal);
                        services.AddSingleton<IExecutionCorrelationIdProvider>(
                            new StaticCorrelationIdProvider(correlationId));
                        services
                            .AddJobGuardian()
                            .UsePostgreSql(
                                Fixture.ConnectionString);

                        services.AddJob<SignalingJob>(
                            jobKey,
                            new JobExecutionPolicy
                            {
                                LeaseDuration =
                                    TimeSpan.FromMinutes(1),
                                HeartbeatInterval =
                                    TimeSpan.FromSeconds(10)
                            });
                    })
                .Build();

        await host.StartAsync();
        await signal.Completed.Task.WaitAsync(
            TimeSpan.FromSeconds(10));

        var historyStore =
            CreateExecutionHistoryStore();

        ExecutionHistoryEntry? history = null;
        var deadline =
            DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var executionId =
                await FindExecutionIdAsync(jobKey);

            if (executionId is not null)
            {
                history =
                    await historyStore.GetAsync(
                        executionId.Value);

                if (history?.Outcome is not null)
                {
                    break;
                }
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(50));
        }

        await host.StopAsync();

        Assert.NotNull(history);
        Assert.Equal(
            ExecutionOutcome.Succeeded,
            history.Outcome);
        Assert.NotNull(history.EndedAtUtc);
        Assert.Equal(jobKey, history.JobKey);
        Assert.Equal(correlationId, history.CorrelationId);
        Assert.NotNull(history.LeaseAcquiredAtUtc);
        Assert.Equal("Production", history.Environment);
        Assert.False(
            string.IsNullOrWhiteSpace(
                history.ApplicationName));
    }

    private async Task<Guid?> FindExecutionIdAsync(
        JobKey jobKey)
    {
        await using var connection =
            new NpgsqlConnection(
                Fixture.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new NpgsqlCommand(
                """
                SELECT execution_id
                FROM jobguardian_execution_history
                WHERE
                    tenant_id = @TenantId
                    AND job_namespace = @JobNamespace
                    AND job_name = @JobName
                """,
                connection);

        command.Parameters.AddWithValue(
            "TenantId",
            jobKey.TenantId);
        command.Parameters.AddWithValue(
            "JobNamespace",
            jobKey.JobNamespace);
        command.Parameters.AddWithValue(
            "JobName",
            jobKey.JobName);

        var result =
            await command.ExecuteScalarAsync();

        return result is Guid executionId
            ? executionId
            : null;
    }

    private ExecutionHistoryEntry CreateHistoryEntry(
        Guid executionId,
        string tenantId,
        DateTimeOffset startedAtUtc,
        ExecutionOutcome? outcome)
    {
        return new ExecutionHistoryEntry
        {
            ExecutionId = executionId,
            JobKey = new JobKey(
                tenantId,
                JobNamespace,
                JobName),
            CorrelationId = Guid.NewGuid(),
            ProtocolVersion = "1.0",
            SchemaVersion = "2.0",
            ApplicationName = "JobGuardian.IntegrationTests",
            Environment = "Test",
            OwnerId = "test-node",
            StartedAtUtc = startedAtUtc,
            EndedAtUtc = outcome is null
                ? null
                : startedAtUtc.AddSeconds(1),
            Outcome = outcome,
            RunType = RunType.Scheduled,
            TriggeredBy = TriggeredBy.Scheduler
        };
    }

    private sealed class ExecutionSignal
    {
        public TaskCompletionSource Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class StaticCorrelationIdProvider
        : IExecutionCorrelationIdProvider
    {
        private readonly Guid _correlationId;

        public StaticCorrelationIdProvider(Guid correlationId)
        {
            _correlationId = correlationId;
        }

        public Guid? GetCorrelationId(JobKey jobKey)
        {
            return _correlationId;
        }
    }

    private sealed class SignalingJob
        : IJob
    {
        private readonly ExecutionSignal _signal;

        public SignalingJob(
            ExecutionSignal signal)
        {
            _signal = signal;
        }

        public Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            _signal.Completed.TrySetResult();
            return Task.CompletedTask;
        }
    }
}
