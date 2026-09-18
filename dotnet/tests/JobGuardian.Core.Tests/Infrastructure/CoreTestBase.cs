using JobGuardian.Abstractions.Contracts;
using JobGuardian.Core.Contracts;
using NSubstitute;

namespace JobGuardian.Core.Tests.Infrastructure;

public abstract class CoreTestBase
{
    protected static ILeaseStore CreateLeaseStore()
    {
        return Substitute.For<ILeaseStore>();
    }

    protected static ILeaseHeartbeatService CreateHeartbeatService()
    {
        return Substitute.For<ILeaseHeartbeatService>();
    }
}