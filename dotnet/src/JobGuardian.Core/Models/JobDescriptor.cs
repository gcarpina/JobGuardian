using JobGuardian.Abstractions.Models;

namespace JobGuardian.Core.Models;

public sealed record JobDescriptor(
    JobKey JobKey,
    Type JobType,
    JobExecutionPolicy Policy);