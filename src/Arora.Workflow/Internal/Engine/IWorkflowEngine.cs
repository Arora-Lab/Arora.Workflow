using Arora.Workflow.Domain.Aggregates;

namespace Arora.Workflow.Internal.Engine;

/// <summary>
/// The internal contract for the workflow execution engine.
/// Not part of the public API surface — host applications use
/// <see cref="Application.Interfaces.IWorkflowService"/> instead.
/// </summary>
/// <remarks>
/// The engine is responsible for:
/// <list type="bullet">
///   <item>Evaluating valid transitions from the current state</item>
///   <item>Executing step implementations</item>
///   <item>Handling retries according to the step's <c>RetryPolicy</c></item>
///   <item>Creating Approval records for PendingApproval states</item>
///   <item>Updating the instance state via <c>WorkflowInstance.TransitionTo()</c></item>
/// </list>
/// The engine does not persist or publish events — it mutates the aggregate only.
/// The caller (WorkflowService / ApprovalService) is responsible for persisting
/// and publishing after each engine call.
/// </remarks>
public interface IWorkflowEngine
{
    /// <summary>
    /// Advances a workflow instance from its current state.
    /// Executes the current step (if automatic) or creates an Approval record
    /// (if the current state is PendingApproval).
    /// </summary>
    /// <param name="instance">The instance to advance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AdvanceAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a specific step for the given workflow instance and transitions
    /// the instance to the next state based on the step's result condition.
    /// This is typically called by a background worker.
    /// </summary>
    Task ExecuteStepAsync(
        WorkflowInstance instance,
        string stepName,
        CancellationToken cancellationToken = default);
}
