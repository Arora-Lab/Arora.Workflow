using Arora.Workflow.Domain.Entities;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;

namespace Arora.Workflow.Testing;

/// <summary>
/// An in-memory <see cref="IWorkflowDefinitionRepository"/> for unit tests.
/// No database. No async overhead. Instant.
/// </summary>
public sealed class InMemoryWorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly Dictionary<Guid, WorkflowDefinition> _store = [];

    /// <summary>All definitions currently in the store. Use in test assertions.</summary>
    public IReadOnlyCollection<WorkflowDefinition> All => _store.Values;

    /// <inheritdoc />
    public Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var definition);
        return Task.FromResult(definition);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> GetLatestPublishedAsync(
        string name, CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .Where(d => d.Name == name
                     && d.Status == Domain.ValueObjects.WorkflowDefinitionStatus.Published)
            .OrderByDescending(d => d.Version)
            .FirstOrDefault();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> GetByNameAndVersionAsync(
        string name, int version, CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .FirstOrDefault(d => d.Name == name && d.Version == version);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        _store[definition.Id] = definition;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        _store[definition.Id] = definition;
        return Task.CompletedTask;
    }
}

/// <summary>
/// An in-memory <see cref="IWorkflowInstanceRepository"/> for unit tests.
/// </summary>
public sealed class InMemoryWorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly Dictionary<Guid, WorkflowInstance> _store = [];

    /// <summary>All instances currently in the store. Use in test assertions.</summary>
    public IReadOnlyCollection<WorkflowInstance> All => _store.Values;

    /// <inheritdoc />
    public Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var instance);
        return Task.FromResult(instance);
    }

    /// <inheritdoc />
    public Task<WorkflowInstance?> GetByCorrelationIdAsync(
        string correlationId, Guid workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .FirstOrDefault(i => i.CorrelationId == correlationId
                              && (workflowDefinitionId == Guid.Empty || i.WorkflowDefinitionId == workflowDefinitionId));

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByIdempotencyKeyAsync(
        string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var exists = _store.Values.Any(i => i.IdempotencyKey == idempotencyKey);
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        _store[instance.Id] = instance;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        _store[instance.Id] = instance;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowInstance>> GetRunningInstancesAsync(CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .Where(i => i.Status == Domain.ValueObjects.WorkflowStatus.Running)
            .ToList();
            
        return Task.FromResult<IReadOnlyList<WorkflowInstance>>(result);
    }
}

/// <summary>
/// A no-op <see cref="IUnitOfWork"/> for unit tests.
/// "Saving" is instant because there is no database.
/// </summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    /// <summary>How many times <see cref="SaveChangesAsync"/> was called. Use in assertions.</summary>
    public int SaveCount { get; private set; }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

/// <summary>
/// An in-memory <see cref="IApprovalRepository"/> for unit tests.
/// </summary>
public sealed class InMemoryApprovalRepository : IApprovalRepository
{
    private readonly Dictionary<Guid, Approval> _store = [];

    /// <summary>All approvals currently in the store. Use in test assertions.</summary>
    public IReadOnlyCollection<Approval> All => _store.Values;

    /// <inheritdoc />
    public Task<Approval?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var approval);
        return Task.FromResult(approval);
    }

    /// <inheritdoc />
    public Task AddAsync(Approval approval, CancellationToken cancellationToken = default)
    {
        _store[approval.Id] = approval;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Approval approval, CancellationToken cancellationToken = default)
    {
        _store[approval.Id] = approval;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Approval?> GetLatestApprovalAsync(Guid workflowInstanceId, string stepName, CancellationToken cancellationToken = default)
    {
        var approval = _store.Values
            .Where(a => a.WorkflowInstanceId == workflowInstanceId && a.StepName == stepName)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefault();
            
        return Task.FromResult(approval);
    }

    public Task<IReadOnlyList<Approval>> GetPendingByActorAsync(
        Arora.Workflow.Domain.ValueObjects.ActorInfo actor,
        CancellationToken cancellationToken = default)
    {
        var approvals = _store.Values
            .Where(x => x.Status == Arora.Workflow.Domain.ValueObjects.ApprovalStatus.Pending && x.AssignedActor?.Id == actor.Id)
            .ToList();

        return Task.FromResult<IReadOnlyList<Approval>>(approvals);
    }
}

public sealed class InMemoryWorkflowHistoryRepository : IWorkflowHistoryRepository
{
    private readonly List<WorkflowHistory> _store = new();
    public Task AddAsync(WorkflowHistory history, CancellationToken cancellationToken = default)
    {
        _store.Add(history);
        return Task.CompletedTask;
    }
    public Task<IReadOnlyList<WorkflowHistory>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var result = _store
            .Where(h => h.WorkflowInstanceId == instanceId)
            .OrderBy(h => h.Timestamp)
            .ToList();
        return Task.FromResult<IReadOnlyList<WorkflowHistory>>(result);
    }
}
