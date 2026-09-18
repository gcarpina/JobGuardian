namespace JobGuardian.Abstractions.Models;

public sealed record OwnerId(
    string Environment,
    string Application,
    string Instance)
{
    public override string ToString()
        => $"{Environment}/{Application}/{Instance}";
}