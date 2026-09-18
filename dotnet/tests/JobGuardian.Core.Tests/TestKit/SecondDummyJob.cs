using JobGuardian.Abstractions.Contracts;

namespace JobGuardian.Core.Tests.TestKit;

internal sealed class SecondDummyJob : IJob
{
    public Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
