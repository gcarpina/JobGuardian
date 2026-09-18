namespace JobGuardian.Abstractions.Contracts;

public interface IExecutionIdentityProvider
{
    string GetOwnerId();
}