using Arora.Workflow.Domain.Aggregates;

namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// Repository for reading and persisting WorkflowDefinition aggregates.
/// Implemented by <c>Arora.Workflow.EntityFramework</c>.
/// </summary>
public interface IWorkflowDefinitionRepository
{
    /// <summary>
    /// Returns a workflow definition by its unique ID, or null if not found.
    /// </summary>
    Task<WorkflowDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the latest published version of a workflow definition by name,
    /// or null if no published definition exists with that name.
    /// </summary>
    Task<WorkflowDefinition?> GetLatestPublishedAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a specific version of a workflow definition by name and version,
    /// or null if not found.
    /// </summary>
    Task<WorkflowDefinition?> GetByNameAndVersionAsync(
        string name,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new WorkflowDefinition to the database.
    /// </summary>
    Task AddAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to an existing WorkflowDefinition.
    /// </summary>
    Task UpdateAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for reading and persisting WorkflowInstance aggregates.
/// Implemented by <c>Arora.Workflow.EntityFramework</c>.
/// </summary>
public interface IWorkflowInstanceRepository
{
    /// <summary>
    /// Returns a workflow instance by its unique ID, or null if not found.
    /// </summary>
    Task<WorkflowInstance?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the workflow instance associated with the given correlation ID
    /// and definition ID, or null if not found.
    /// </summary>
    /// <remarks>
    /// The uniqueness constraint (TenantId, CorrelationId, WorkflowDefinitionId)
    /// ensures at most one active instance per business entity per workflow type.
    /// </remarks>
    Task<WorkflowInstance?> GetByCorrelationIdAsync(
        string correlationId,
        Guid workflowDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a workflow instance already exists for the given idempotency key.
    /// Used to enforce idempotent starts without loading the full aggregate.
    /// </summary>
    Task<bool> ExistsByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new WorkflowInstance to the database.
    /// </summary>
    Task AddAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to an existing WorkflowInstance.
    /// </summary>
    Task UpdateAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns instances that are currently in the Running status.
    /// </summary>
    Task<IReadOnlyList<WorkflowInstance>> GetRunningInstancesAsync(
        CancellationToken cancellationToken = default);
}
