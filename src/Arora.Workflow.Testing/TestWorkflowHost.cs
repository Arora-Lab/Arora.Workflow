using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Application.Services;
using Arora.Workflow.Internal.Engine;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arora.Workflow.Testing;

/// <summary>
/// A self-contained test harness for the Arora.Workflow engine.
/// Wires all fakes together and provides a fully functional
/// <see cref="IWorkflowService"/> and <see cref="IApprovalService"/>
/// backed by in-memory repositories.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// var host = new TestWorkflowHost();
///
/// // Seed a definition
/// await host.DefinitionRepository.AddAsync(myDefinition);
///
/// // Call the service under test
/// var result = await host.WorkflowService.StartAsync(new StartWorkflowRequest { ... });
///
/// // Assert on the repository state
/// var instance = host.InstanceRepository.All.Single();
/// Assert.Equal("Initial", instance.CurrentState);
/// </code>
/// </remarks>
public sealed class TestWorkflowHost
{
    // ── Fakes exposed for seeding and assertions ─────────────────────────────

    /// <summary>The in-memory definition repository. Seed before calling services.</summary>
    public InMemoryWorkflowDefinitionRepository DefinitionRepository { get; } = new();

    /// <summary>The in-memory instance repository. Assert on state after service calls.</summary>
    public InMemoryWorkflowInstanceRepository InstanceRepository { get; } = new();

    /// <summary>The in-memory approval repository. Assert on approval states after service calls.</summary>
    public InMemoryApprovalRepository ApprovalRepository { get; } = new();

    /// <summary>The in-memory unit of work. Assert on save counts.</summary>
    public InMemoryUnitOfWork UnitOfWork { get; } = new();

    /// <summary>The controllable clock. Advance time between operations.</summary>
    public FakeClock Clock { get; } = new();

    /// <summary>The fixed tenant context.</summary>
    public FakeTenantContext TenantContext { get; } = new();

    // ── Services under test ──────────────────────────────────────────────────

    /// <summary>The fully wired <see cref="IWorkflowService"/> backed by in-memory fakes.</summary>
    public IWorkflowService WorkflowService { get; }

    /// <summary>The fully wired <see cref="IApprovalService"/> backed by in-memory fakes.</summary>
    public IApprovalService ApprovalService { get; }

    public TestWorkflowHost()
    {
        // Build a minimal DI container for MediatR (used for event publishing)
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AroraWorkflowBuilder).Assembly));
        var serviceProvider = services.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IPublisher>();
        var engine    = new WorkflowEngine(DefinitionRepository, ApprovalRepository, Clock, serviceProvider);

        WorkflowService = new WorkflowService(
            definitionRepo: DefinitionRepository,
            instanceRepo:   InstanceRepository,
            engine:         engine,
            uow:            UnitOfWork,
            tenantContext:  TenantContext,
            clock:          Clock,
            publisher:      publisher,
            logger:         NullLogger<WorkflowService>.Instance);

        ApprovalService = new ApprovalService(
            approvalRepo:  ApprovalRepository,
            instanceRepo:  InstanceRepository,
            engine:        engine,
            uow:           UnitOfWork,
            tenantContext: TenantContext,
            clock:         Clock,
            publisher:     publisher,
            logger:        NullLogger<ApprovalService>.Instance);
    }
}
