using System.Diagnostics;
using System.Diagnostics.Metrics;
using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Observability;

internal static class JobGuardianTelemetry
{
    private static readonly ActivitySource ActivitySource =
        new(JobGuardianInstrumentation.ActivitySourceName);

    private static readonly Meter Meter =
        new(JobGuardianInstrumentation.MeterName);

    private static long _activeLeases;

    private static readonly Counter<long> ExecutionStarts =
        Meter.CreateCounter<long>(
            "jobguardian.execution.started",
            unit: "{attempt}",
            description: "Number of JobGuardian coordination attempts started.");

    private static readonly Counter<long> ExecutionAttempts =
        Meter.CreateCounter<long>(
            "jobguardian.execution.attempts",
            unit: "{attempt}",
            description: "Number of completed JobGuardian execution attempts.");

    private static readonly Counter<long> ExecutionRetries =
        Meter.CreateCounter<long>(
            "jobguardian.execution.retries",
            unit: "{retry}",
            description: "Number of callback retries scheduled after a failed attempt.");

    private static readonly Histogram<double> ExecutionDuration =
        Meter.CreateHistogram<double>(
            "jobguardian.execution.duration",
            unit: "s",
            description: "Duration of JobGuardian execution attempts.");

    private static readonly UpDownCounter<long> ActiveExecutions =
        Meter.CreateUpDownCounter<long>(
            "jobguardian.execution.active",
            unit: "{execution}",
            description: "Number of execution attempts currently being coordinated.");

    private static readonly ObservableGauge<long> ActiveLeases =
        Meter.CreateObservableGauge(
            "jobguardian.lease.active",
            () => Interlocked.Read(ref _activeLeases),
            unit: "{lease}",
            description: "Number of leases currently held by this process.");

    private static readonly Counter<long> LeaseOperations =
        Meter.CreateCounter<long>(
            "jobguardian.lease.operations",
            unit: "{operation}",
            description: "Number of JobGuardian lease operations by operation and result.");

    private static readonly Histogram<double> LeaseOperationDuration =
        Meter.CreateHistogram<double>(
            "jobguardian.lease.operation.duration",
            unit: "s",
            description: "Duration of JobGuardian lease operations.");

    public static Activity? StartActivity(
        string name,
        ActiveExecution execution)
    {
        var activity =
            ActivitySource.StartActivity(name);

        activity?.SetTag(
            "jobguardian.job.namespace",
            execution.JobKey.JobNamespace);
        activity?.SetTag(
            "jobguardian.job.name",
            execution.JobKey.JobName);
        activity?.SetTag(
            "jobguardian.execution.id",
            execution.ExecutionId);

        return activity;
    }

    public static void ExecutionStarted()
    {
        ExecutionStarts.Add(1);
        ActiveExecutions.Add(1);
    }

    public static void ExecutionRetryScheduled()
    {
        ExecutionRetries.Add(1);
    }

    public static void LeaseAcquired()
    {
        Interlocked.Increment(ref _activeLeases);
    }

    public static void LeaseReleased()
    {
        Interlocked.Decrement(ref _activeLeases);
    }

    public static void ExecutionCompleted(
        string outcome,
        double durationSeconds)
    {
        ActiveExecutions.Add(-1);
        ExecutionAttempts.Add(
            1,
            new KeyValuePair<string, object?>(
                "outcome",
                outcome));
        ExecutionDuration.Record(
            durationSeconds,
            new KeyValuePair<string, object?>(
                "outcome",
                outcome));
    }

    public static Activity? StartLeaseActivity(
        string operation,
        ActiveExecution execution)
    {
        var activity =
            StartActivity(
                $"JobGuardian.{operation}Lease",
                execution);

        activity?.SetTag(
            "jobguardian.lease.operation",
            operation.ToLowerInvariant());

        return activity;
    }

    public static void LeaseOperationCompleted(
        string operation,
        string result,
        double durationSeconds)
    {
        LeaseOperations.Add(
            1,
            new KeyValuePair<string, object?>(
                "operation",
                operation),
            new KeyValuePair<string, object?>(
                "result",
                result));
        LeaseOperationDuration.Record(
            durationSeconds,
            new KeyValuePair<string, object?>(
                "operation",
                operation),
            new KeyValuePair<string, object?>(
                "result",
                result));
    }
}
