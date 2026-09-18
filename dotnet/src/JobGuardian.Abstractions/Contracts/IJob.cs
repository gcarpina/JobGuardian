namespace JobGuardian.Abstractions.Contracts;

public interface IJob
{
    Task ExecuteAsync(
        CancellationToken cancellationToken);
}