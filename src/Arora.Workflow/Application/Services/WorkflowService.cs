using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.Events;
using Arora.Workflow.Domain.Exceptions;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.Internal.Engine;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Arora.Workflow.Application.Services;

/// <summary>
/// The concrete implementation of <see cref="IWorkflowService"/>.
/// Coordinates repositories, the engine, and event publication.
/// </summary>
internal sealed class WorkflowService : IWorkflowService
{
    private readonly IWorkflowDefinitionRepository _definitionRepo;
    private readonly IWorkflowInstanceRepository _instanceRepo;
    private readonly IWorkflowEngine _engine;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenantContext;
    private readonly IWorkflowClock _clock;
    private readonly IPublisher _publisher;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowDefinitionRepository definitionRepo,
        IWorkflowInstanceRepository instanceRepo,
        IWorkflowEngine engine,
        IUnitOfWork uow,
        ITenantContext tenantContext,
        IWorkflowClock clock,
        IPublisher publisher,
        ILogger<WorkflowService> logger)
    {
        _definitionRepo = definitionRepo;
        _instanceRepo   = instanceRepo;
        _engine         = engine;
        _uow            = uow;
        _tenantContext  = tenantContext;
        _clock          = clock;
        _publisher      = publisher;
        _logger         = logger;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstanceSnapshot> StartAsync(
        StartWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        // ── 1. Idempotency check ─────────────────────────────────────────────
        // If an instance already exists for this key, return it immediately.
        // This makes StartAsync safe to call multiple times (e.g., on retry).
        if (await _instanceRepo.ExistsByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken))
        {
            var existing = await _instanceRepo.GetByCorrelationIdAsync(
                request.CorrelationId,
                Guid.Empty, // we look up by correlation+name since we don't have the definitionId yet
                cancellationToken);

            if (existing is not null)
            {
                _logger.LogDebug(
                    "Workflow start skipped — instance already exists for idempotency key {Key}",
                    request.IdempotencyKey);

                return WorkflowMapper.ToSnapshot(existing);
            }
        }

        // ── 2. Resolve the workflow definition ───────────────────────────────
        WorkflowDefinition? definition = request.Version.HasValue
            ? await _definitionRepo.GetByNameAndVersionAsync(
                request.WorkflowName, request.Version.Value, cancellationToken)
            : await _definitionRepo.GetLatestPublishedAsync(
                request.WorkflowName, cancellationToken);

        if (definition is null)
            throw new WorkflowDefinitionNotFoundException(
                request.WorkflowName, request.Version);

        if (!definition.CanStartNewInstances())
            throw new WorkflowDefinitionNotFoundException(
                request.WorkflowName, request.Version);

        // ── 3. Create the aggregate ──────────────────────────────────────────
        // The initial state is always "Initial". The engine will immediately
        // advance past it if the first real state is automatic.
        var initialState = new WorkflowState("Initial", WorkflowStateType.Initial);

        var instance = WorkflowInstance.Start(
            tenantId:              tenantId,
            definitionId:          definition.Id,
            definitionVersion:     definition.Version,
            workflowName:          definition.Name,
            correlationId:         request.CorrelationId,
            idempotencyKey:        request.IdempotencyKey,
            initialState:          initialState,
            inputJson:             request.Input is null ? null : System.Text.Json.JsonSerializer.Serialize(request.Input),
            initiatedBy:           request.InitiatedBy,
            clock:                 _clock.UtcNow);

        // ── 4. Persist the new instance ──────────────────────────────────────
        // Persist first. If the engine call below fails and we retry, the
        // idempotency check in step 1 prevents a duplicate from being created.
        await _instanceRepo.AddAsync(instance, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // ── 5. Publish domain events raised during Start() ───────────────────
        // Events are published AFTER the database commit. If publishing fails,
        // the state is still safely persisted.
        await PublishAndClearEventsAsync(instance, cancellationToken);

        // ── 6. Advance the engine ────────────────────────────────────────────
        // This executes the first step or creates the first approval record.
        await _engine.AdvanceAsync(instance, cancellationToken);

        // ── 7. Persist state changes made by the engine ─────────────────────
        await _instanceRepo.UpdateAsync(instance, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // ── 8. Publish events raised during engine execution ─────────────────
        await PublishAndClearEventsAsync(instance, cancellationToken);

        _logger.LogInformation(
            "Workflow {Name} started. InstanceId={InstanceId}, CorrelationId={CorrelationId}",
            definition.Name, instance.Id, request.CorrelationId);

        return WorkflowMapper.ToSnapshot(instance);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstanceSnapshot?> GetInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken);
        return instance is null ? null : WorkflowMapper.ToSnapshot(instance);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstanceSnapshot?> GetByCorrelationIdAsync(
        string correlationId,
        string workflowName,
        CancellationToken cancellationToken = default)
    {
        var definition = await _definitionRepo.GetLatestPublishedAsync(workflowName, cancellationToken);
        if (definition is null) return null;

        var instance = await _instanceRepo.GetByCorrelationIdAsync(
            correlationId, definition.Id, cancellationToken);

        return instance is null ? null : WorkflowMapper.ToSnapshot(instance);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowHistoryEntry>> GetHistoryAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        // Verify the instance exists before returning an empty history list.
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            throw new WorkflowNotFoundException(instanceId);

        // History is stored separately — the repository has a history-specific query.
        // For now, return an empty list; the history repository is wired in Phase 2.
        return Array.Empty<WorkflowHistoryEntry>();
    }

    /// <inheritdoc />
    public async Task CancelAsync(
        Guid instanceId,
        string reason,
        ActorInfo cancelledBy,
        CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            throw new WorkflowNotFoundException(instanceId);

        // Cancel() on the aggregate is idempotent — if already terminal, it's a no-op.
        instance.Cancel(reason, cancelledBy, _clock.UtcNow);

        await _instanceRepo.UpdateAsync(instance, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await PublishAndClearEventsAsync(instance, cancellationToken);

        _logger.LogInformation(
            "Workflow instance {InstanceId} cancelled. Reason: {Reason}",
            instanceId, reason);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Publishes all collected domain events from the instance, then clears them.
    /// Events are published after SaveChanges — never before.
    /// </summary>
    private async Task PublishAndClearEventsAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in instance.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        instance.ClearDomainEvents();
    }
}
