using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Raised when a WorkflowInstance reaches a <c>Completed</c> terminal state.
/// This is the "happy path" end event.
/// </summary>
/// <param name="WorkflowInstanceId">The instance that completed.</param>
/// <param name="WorkflowName">The workflow definition name.</param>
/// <param name="CorrelationId">The business entity reference.</param>
/// <param name="TotalDurationMs">Total wall-clock time from start to completion.</param>
/// <param name="OccurredAt">The UTC time of completion.</param>
public sealed record WorkflowCompleted(
    Guid WorkflowInstanceId,
    string WorkflowName,
    string CorrelationId,
    int TotalDurationMs,
    DateTimeOffset OccurredAt) : IWorkflowEvent;

/// <summary>
/// Raised when a WorkflowInstance reaches a <c>Rejected</c> terminal state
/// because an approver rejected it.
/// </summary>
/// <param name="WorkflowInstanceId">The instance that was rejected.</param>
/// <param name="WorkflowName">The workflow definition name.</param>
/// <param name="CorrelationId">The business entity reference.</param>
/// <param name="RejectedAtStep">The name of the approval step where rejection occurred.</param>
/// <param name="RejectedBy">The actor who submitted the rejection.</param>
/// <param name="OccurredAt">The UTC time of rejection.</param>
public sealed record WorkflowRejected(
    Guid WorkflowInstanceId,
    string WorkflowName,
    string CorrelationId,
    string RejectedAtStep,
    ActorInfo RejectedBy,
    DateTimeOffset OccurredAt) : IWorkflowEvent;

/// <summary>
/// Raised when a WorkflowInstance is manually cancelled before reaching
/// a natural terminal state.
/// </summary>
/// <param name="WorkflowInstanceId">The instance that was cancelled.</param>
/// <param name="WorkflowName">The workflow definition name.</param>
/// <param name="CorrelationId">The business entity reference.</param>
/// <param name="Reason">A human-readable explanation of why it was cancelled.</param>
/// <param name="CancelledBy">The actor who initiated the cancellation.</param>
/// <param name="OccurredAt">The UTC time of cancellation.</param>
public sealed record WorkflowCancelled(
    Guid WorkflowInstanceId,
    string WorkflowName,
    string CorrelationId,
    string Reason,
    ActorInfo CancelledBy,
    DateTimeOffset OccurredAt,
    string LastActiveState,
    string LastActiveNodeId,
    string? CancelledNodeId) : IWorkflowEvent;
