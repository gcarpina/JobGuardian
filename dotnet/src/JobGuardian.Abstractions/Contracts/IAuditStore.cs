using JobGuardian.Abstractions.Models;

namespace JobGuardian.Abstractions.Contracts;

public interface IAuditStore
{
    Task WriteAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default);
}