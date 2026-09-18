using JobGuardian.Abstractions.Contracts;

namespace JobGuardian.Core.Tests.TestKit;

internal sealed class DummyJob : IJob
{
    public bool Executed
    {
        get;
        private set;
    }

    public Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        Executed = true;

        return Task.CompletedTask;
    }
}