using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// The service for managing approval decisions on workflow instances.
/// Inject this interface into your approval controllers, API endpoints, and
/// Teams / Slack webhook handlers.
/// </summary>
/// <remarks>
/// <para>Lifetime: Scoped (one instance per DI scope / HTTP request).</para>
/// <para>
/// When an actor submits an approval or rejection, this service:
/// <list type="number">
///   <item>Validates the approval exists and is still pending</item>
///   <item>Records the decision in the database</item>
///   <item>Resumes the workflow engine for the associated instance</item>
///   <item>Publishes the appropriate domain event</item>
/// </list>
/// </para>
/// </remarks>
public interface IApprovalService
{
    /// <summary>
    /// Returns all pending approvals currently assigned to the specified actor.
    /// </summary>
    /// <param name="actor">The actor whose pending approvals to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of pending approvals assigned to this actor, ordered by creation time.
    /// Returns an empty list if the actor has no pending approvals.
    /// </returns>
    Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync(
        ActorInfo actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a specific approval by ID, or null if not found.
    /// </summary>
    /// <param name="approvalId">The unique ID of the approval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PendingApproval?> GetApprovalAsync(
        Guid approvalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits an approval decision, resuming workflow execution.
    /// </summary>
    /// <param name="approvalId">The ID of the approval to decide.</param>
    /// <param name="actor">The actor submitting the decision.</param>
    /// <param name="comment">An optional comment explaining the decision.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.WorkflowNotFoundException">
    /// Thrown when the approval's workflow instance no longer exists.
    /// </exception>
    /// <exception cref="Exceptions.DuplicateApprovalException">
    /// Thrown when this approval has already been decided.
    /// </exception>
    /// <exception cref="Exceptions.WorkflowInTerminalStateException">
    /// Thrown when the workflow instance is already in a terminal state.
    /// </exception>
    Task ApproveAsync(
        Guid approvalId,
        ActorInfo actor,
        string? comment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a rejection decision, routing the workflow to its rejection path.
    /// </summary>
    /// <param name="approvalId">The ID of the approval to reject.</param>
    /// <param name="actor">The actor submitting the rejection.</param>
    /// <param name="comment">An optional comment explaining the rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.WorkflowNotFoundException">
    /// Thrown when the approval's workflow instance no longer exists.
    /// </exception>
    /// <exception cref="Exceptions.DuplicateApprovalException">
    /// Thrown when this approval has already been decided.
    /// </exception>
    /// <exception cref="Exceptions.WorkflowInTerminalStateException">
    /// Thrown when the workflow instance is already in a terminal state.
    /// </exception>
    Task RejectAsync(
        Guid approvalId,
        ActorInfo actor,
        string? comment = null,
        CancellationToken cancellationToken = default);
}
