using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Tests.Models;

public sealed class PublicModelTests
{
    [Fact]
    public void AuditEntry_Should_Expose_Initialized_Values()
    {
        var auditId =
            Guid.NewGuid();
        var timestamp =
            DateTimeOffset.UtcNow;
        var executionId =
            Guid.NewGuid();
        var entry =
            new AuditEntry
            {
                AuditId = auditId,
                TenantId = "tenant-a",
                TimestampUtc = timestamp,
                UserId = "service-account",
                UserName = "JobGuardian",
                Action = "ResetJob",
                JobNamespace = "billing",
                JobName = "invoice-sync",
                ExecutionId = executionId,
                DetailsJson = "{\"reason\":\"reconciled\"}"
            };

        Assert.Equal(auditId, entry.AuditId);
        Assert.Equal("tenant-a", entry.TenantId);
        Assert.Equal(timestamp, entry.TimestampUtc);
        Assert.Equal("service-account", entry.UserId);
        Assert.Equal("JobGuardian", entry.UserName);
        Assert.Equal("ResetJob", entry.Action);
        Assert.Equal("billing", entry.JobNamespace);
        Assert.Equal("invoice-sync", entry.JobName);
        Assert.Equal(executionId, entry.ExecutionId);
        Assert.Equal("{\"reason\":\"reconciled\"}", entry.DetailsJson);
    }

    [Fact]
    public void OwnerId_Should_Expose_Components_And_Format_Identity()
    {
        var owner =
            new OwnerId(
                "production",
                "billing-api",
                "worker-07");

        Assert.Equal("production", owner.Environment);
        Assert.Equal("billing-api", owner.Application);
        Assert.Equal("worker-07", owner.Instance);
        Assert.Equal("production/billing-api/worker-07", owner.ToString());
    }
}
