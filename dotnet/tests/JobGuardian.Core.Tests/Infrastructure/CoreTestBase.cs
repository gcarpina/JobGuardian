using JobGuardian.Abstractions.Contracts;
using JobGuardian.Core.Contracts;
using NSubstitute;

namespace JobGuardian.Core.Tests.Infrastructure;

public abstract class CoreTestBase
{
    protected static ILeaseStore CreateLeaseStore()
    {
        var leaseStore =
            Substitute.For<ILeaseStore>();

        leaseStore
            .ReleaseAsync(
                Arg.Any<JobGuardian.Abstractions.Models.JobKey>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        return leaseStore;
    }

    protected static ILeaseHeartbeatService CreateHeartbeatService()
    {
        return Substitute.For<ILeaseHeartbeatService>();
    }
}