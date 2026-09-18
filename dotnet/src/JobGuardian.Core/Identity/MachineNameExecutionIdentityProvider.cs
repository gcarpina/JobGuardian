using JobGuardian.Abstractions.Contracts;

namespace JobGuardian.Core.Identity;

internal sealed class MachineNameExecutionIdentityProvider
    : IExecutionIdentityProvider
{
    public string GetOwnerId()
    {
        return Environment.MachineName;
    }
}