using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Models;
using JobGuardian.Core.Observability;
using JobGuardian.Core.Tests.Infrastructure;
using NSubstitute;

namespace JobGuardian.Core.Tests.Observability;

public sealed class JobGuardianTelemetryTests
    : CoreTestBase
{
    [Fact]
    public async Task CT1200_Execution_Should_Emit_Metrics_And_Activities()
    {
        var execution =
            new ActiveExecution
            {
                JobKey = new JobKey(
                    "tenant-otel-test",
                    "observability",
                    $"job-{Guid.NewGuid():N}"),
                ExecutionId = Guid.NewGuid(),
                OwnerId = "otel-test-owner",
                CorrelationId = Guid.NewGuid()
            };

        var measurements =
            new ConcurrentBag<(string Name, string? Outcome, string? Operation, string? Result)>();

        var activeLeaseValues =
            new ConcurrentBag<long>();

        var metricTagKeys =
            new ConcurrentBag<string[]>();

        using var meterListener =
            new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name
                        == JobGuardianInstrumentation.MeterName)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };

        meterListener.SetMeasurementEventCallback<long>(
            (instrument, measurementValue, tags, _) =>
            {
                metricTagKeys.Add(
                    GetTagKeys(tags));
                measurements.Add(
                    (
                        instrument.Name,
                        GetTag(tags, "outcome"),
                        GetTag(tags, "operation"),
                        GetTag(tags, "result")));

                if (instrument.Name == "jobguardian.lease.active")
                {
                    activeLeaseValues.Add(measurementValue);
                }
            });

        meterListener.SetMeasurementEventCallback<double>(
            (instrument, _, tags, _) =>
            {
                metricTagKeys.Add(
                    GetTagKeys(tags));
                measurements.Add(
                    (
                        instrument.Name,
                        GetTag(tags, "outcome"),
                        GetTag(tags, "operation"),
                        GetTag(tags, "result")));
            });

        meterListener.Start();

        var activities =
            new ConcurrentBag<(
                string Name,
                string? JobName,
                string? ParentId,
                string? CorrelationId)>();

        using var activityListener =
            new ActivityListener
            {
                ShouldListenTo = source =>
                    source.Name
                    == JobGuardianInstrumentation.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (activity.GetTagItem(
                            "jobguardian.execution.id")?.ToString()
                        == execution.ExecutionId.ToString())
                    {
                        activities.Add(
                            (
                                activity.OperationName,
                                activity.GetTagItem(
                                    "jobguardian.job.name")?.ToString(),
                                activity.ParentId,
                                activity.GetTagItem(
                                    "jobguardian.correlation.id")?.ToString()));
                    }
                }
            };

        ActivitySource.AddActivityListener(activityListener);

        var leaseStore =
            CreateLeaseStore();

        leaseStore
            .TryAcquireAsync(
                execution,
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        leaseStore
            .RenewAsync(
                execution.JobKey,
                execution.ExecutionId,
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var coordinator =
            new JobExecutionCoordinator(
                leaseStore,
                new LeaseHeartbeatService(leaseStore));

        var options =
            new JobExecutionOptions
            {
                LeaseDuration = TimeSpan.FromSeconds(30),
                HeartbeatInterval = TimeSpan.FromMilliseconds(5)
            };

        using var hostActivity =
            new Activity("Host.Operation").Start();

        var result =
            await coordinator.ExecuteAsync(
                execution,
                options,
                async cancellationToken =>
                {
                    meterListener.RecordObservableInstruments();

                    await Task.Delay(
                        TimeSpan.FromMilliseconds(25),
                        cancellationToken);
                });

        meterListener.RecordObservableInstruments();

        Assert.Equal(
            ExecutionOutcome.Succeeded,
            result.Outcome);
        Assert.Contains(
            measurements,
            measurement =>
                measurement.Name == "jobguardian.execution.started");
        Assert.Contains(
            measurements,
            measurement =>
                measurement.Name == "jobguardian.execution.attempts"
                && measurement.Outcome == nameof(ExecutionOutcome.Succeeded));
        Assert.Contains(
            measurements,
            measurement =>
                measurement.Name == "jobguardian.execution.duration"
                && measurement.Outcome == nameof(ExecutionOutcome.Succeeded));
        Assert.Contains(
            measurements,
            measurement =>
                measurement.Name == "jobguardian.lease.operations"
                && measurement.Operation == "acquire"
                && measurement.Result == "acquired");
        Assert.Contains(
            measurements,
            measurement =>
                measurement.Name == "jobguardian.lease.operations"
                && measurement.Operation == "renew"
                && measurement.Result == "renewed");
        Assert.Contains(
            measurements,
            measurement =>
                measurement.Name == "jobguardian.lease.operations"
                && measurement.Operation == "release"
                && measurement.Result == "released");
        Assert.Contains(
            activeLeaseValues,
            activeLeases => activeLeases > 0);
        Assert.All(
            activeLeaseValues,
            activeLeases => Assert.True(activeLeases >= 0));
        Assert.Contains(
            activities,
            activity =>
                activity.Name == "JobGuardian.ExecutionAttempt");
        Assert.Contains(
            activities,
            activity =>
                activity.Name == "JobGuardian.ExecutionAttempt"
                && activity.ParentId == hostActivity.Id
                && activity.CorrelationId == execution.CorrelationId.ToString());
        Assert.Contains(
            activities,
            activity =>
                activity.Name == "JobGuardian.AcquireLease");
        Assert.Contains(
            activities,
            activity =>
                activity.Name == "JobGuardian.ExecuteJob");
        Assert.Contains(
            activities,
            activity =>
                activity.Name == "JobGuardian.RenewLease");
        Assert.Contains(
            activities,
            activity =>
                activity.Name == "JobGuardian.ReleaseLease");
        Assert.All(
            activities,
            activity =>
                Assert.Equal(
                    execution.JobKey.JobName,
                    activity.JobName));
        Assert.All(
            metricTagKeys.SelectMany(keys => keys),
            key => Assert.DoesNotContain(
                key,
                new[]
                {
                    "tenant.id",
                    "job.id",
                    "job.name",
                    "job.namespace",
                    "execution.id",
                    "correlation.id",
                    "error.message"
                }));
    }

    private static string? GetTag(
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        string key)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key)
            {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private static string[] GetTagKeys(
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var keys = new string[tags.Length];

        for (var index = 0; index < tags.Length; index++)
        {
            keys[index] = tags[index].Key;
        }

        return keys;
    }
}
