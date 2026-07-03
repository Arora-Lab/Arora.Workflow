namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// The primary service for managing workflow instances.
/// Inject this interface into your controllers, command handlers, and background services.
/// </summary>
/// <remarks>
/// <para>Lifetime: Scoped (one instance per DI scope / HTTP request).</para>
/// <para>
/// This interface represents the write side of the workflow API.
/// For reading instance state, use <see cref="GetInstanceAsync"/> and
/// <see cref="GetHistoryAsync"/>.
/// </para>
/// </remarks>
public interface IWorkflowService
{
    /// <summary>
    /// Starts a new workflow instance, or returns the existing instance if one
    /// already exists for the provided <see cref="StartWorkflowRequest.IdempotencyKey"/>.
    /// </summary>
    /// <param name="request">
    /// The start request. <see cref="StartWorkflowRequest.IdempotencyKey"/> must be
    /// unique per tenant. If an instance already exists for the key, it is returned
    /// without creating a new one.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or existing workflow instance snapshot.</returns>
    /// <exception cref="Exceptions.WorkflowDefinitionNotFoundException">
    /// Thrown when no published definition exists for the specified name and version.
    /// </exception>
    Task<WorkflowInstanceSnapshot> StartAsync(
        StartWorkflowRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current snapshot of a workflow instance, or null if not found.
    /// </summary>
    /// <param name="instanceId">The unique ID of the instance to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The <see cref="WorkflowInstanceSnapshot"/>, or null if no instance
    /// with this ID exists in the current tenant.
    /// </returns>
    Task<WorkflowInstanceSnapshot?> GetInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the workflow instance associated with the given correlation ID
    /// and workflow name, or null if not found.
    /// </summary>
    /// <param name="correlationId">The business entity reference.</param>
    /// <param name="workflowName">The workflow definition name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<WorkflowInstanceSnapshot?> GetByCorrelationIdAsync(
        string correlationId,
        string workflowName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the complete, ordered audit history for a workflow instance.
    /// </summary>
    /// <param name="instanceId">The ID of the instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An ordered list of history entries, oldest first.
    /// Returns an empty list if the instance exists but has no history entries.
    /// </returns>
    /// <exception cref="Exceptions.WorkflowNotFoundException">
    /// Thrown when no instance with this ID exists in the current tenant.
    /// </exception>
    Task<IReadOnlyList<WorkflowHistoryEntry>> GetHistoryAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running workflow instance.
    /// Cancellation is idempotent — calling this on an already-cancelled instance
    /// is a no-op.
    /// </summary>
    /// <param name="instanceId">The ID of the instance to cancel.</param>
    /// <param name="reason">A human-readable explanation of why it was cancelled.</param>
    /// <param name="cancelledBy">The actor requesting the cancellation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.WorkflowNotFoundException">
    /// Thrown when no instance with this ID exists in the current tenant.
    /// </exception>
    Task CancelAsync(
        Guid instanceId,
        string reason,
        Domain.ValueObjects.ActorInfo cancelledBy,
        CancellationToken cancellationToken = default);
}
