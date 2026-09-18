using Microsoft.Extensions.Logging;

namespace JobGuardian.Core.Tests.TestKit;

internal sealed class TestLogger<T>
    : ILogger<T>
{
    public List<LogLevel> Levels { get; } = [];

    public List<Exception?> Exceptions { get; } = [];

    public List<string>
        Messages
    {
        get;
    } = [];

    public sealed record LogEntry(
        LogLevel Level,
        string Message,
        Exception? Exception);

    public List<LogEntry>
        Entries
    {
        get;
    } = [];

    public IDisposable BeginScope<TState>(
        TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(
        LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Levels.Add(
            logLevel);

        Exceptions.Add(
            exception);

        Messages.Add(
            formatter(
                state,
                exception));

        Entries.Add(
            new LogEntry(
                logLevel,
                formatter(
                    state,
                    exception),
                exception));
    }

    private sealed class NullScope
        : IDisposable
    {
        public static readonly NullScope
            Instance =
                new();

        public void Dispose()
        {
        }
    }
}