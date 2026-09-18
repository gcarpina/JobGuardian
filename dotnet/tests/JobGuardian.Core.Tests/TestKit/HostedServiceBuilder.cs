using JobGuardian.Abstractions.Contracts;
using JobGuardian.Abstractions.Enums;
using JobGuardian.Abstractions.Models;
using JobGuardian.Core.Execution;
using JobGuardian.Core.Hosting;
using JobGuardian.Core.Models;
using JobGuardian.Core.Options;
using JobGuardian.Core.Tests.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using JobGuardian.Core.State;

internal sealed class HostedServiceBuilder
{
    private readonly IJobExecutionCoordinator
        _coordinator;

    private readonly IServiceProvider
        _serviceProvider;

    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly IServiceScope
        _scope;

    private readonly IExecutionIdentityProvider
        _identityProvider;

    private readonly IJobStateManager
        _stateManager;

    private readonly List<JobDescriptor>
        _jobs = [];

    private string _ownerId =
        "test-node";

    private RuntimeOptions _runtimeOptions =
        TestDefaults.DefaultRuntimeOptions;

    private ILogger<JobGuardianHostedService>
        _logger;

    public HostedServiceBuilder()
    {
        _coordinator =
            Substitute.For<IJobExecutionCoordinator>();

        _serviceProvider =
            Substitute.For<IServiceProvider>();

        _scope =
            Substitute.For<IServiceScope>();

        _scopeFactory =
            Substitute.For<IServiceScopeFactory>();

        _identityProvider =
            Substitute.For<IExecutionIdentityProvider>();

        _stateManager =
            Substitute.For<IJobStateManager>();

        _stateManager
            .GetCurrentStateAsync(
                Arg.Any<JobKey>(),
                Arg.Any<CancellationToken>())
            .Returns(
                JobState.Eligible);

        _identityProvider
            .GetOwnerId()
            .Returns(
                x => _ownerId);

        _scope.ServiceProvider
            .Returns(_serviceProvider);

        _scopeFactory
            .CreateScope()
            .Returns(_scope);

        _logger =
            NullLogger<JobGuardianHostedService>
                .Instance;
    }

    public HostedServiceBuilder AddJob<TJob>(
        TJob instance)
        where TJob : class, IJob
    {
        _serviceProvider
            .GetService(typeof(TJob))
            .Returns(instance);

        _jobs.Add(
            new JobDescriptor(
                new JobKey(
                    "tenant-a",
                    "finance",
                    typeof(TJob).Name),
                typeof(TJob),
                TestDefaults.DefaultPolicy));

        return this;
    }

    public HostedServiceBuilder AddJob<TJob>()
        where TJob : class, IJob, new()
    {
        return AddJob(
            new TJob());
    }

    public HostedServiceBuilder WithOwnerId(
        string ownerId)
    {
        _ownerId =
            ownerId;

        return this;
    }

    public HostedServiceBuilder WithRuntimeOptions(
        RuntimeOptions runtimeOptions)
    {
        _runtimeOptions =
            runtimeOptions;

        return this;
    }

    public HostedServiceBuilder WithLogger(
        ILogger<JobGuardianHostedService> logger)
    {
        _logger =
            logger;

        return this;
    }

    public HostedServiceBuilder WithJobState(
        JobState state)
    {
        _stateManager
            .GetCurrentStateAsync(
                Arg.Any<JobKey>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(state));

        return this;
    }

    public HostedServiceContext Build()
    {
        var hostedService =
            new JobGuardianHostedService(
                _jobs,
                _scopeFactory,
                _coordinator,
                _stateManager,
                _identityProvider,
                _runtimeOptions,
                _logger);

        return new HostedServiceContext
        {
            HostedService = hostedService,
            Coordinator = _coordinator,
            StateManager = _stateManager,
            ServiceProvider = _serviceProvider,
            ScopeFactory = _scopeFactory,
            Scope = _scope,
            IdentityProvider = _identityProvider
        };
    }
}