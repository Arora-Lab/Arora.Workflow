using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// A read-only snapshot of a WorkflowInstance's current state.
/// Returned by <see cref="IWorkflowService"/> queries.
/// </summary>
public sealed record WorkflowInstanceSnapshot
{
    /// <summary>The unique identifier of the workflow instance.</summary>
    public required Guid Id { get; init; }

    /// <summary>The name of the workflow definition this instance is executing.</summary>
    public required string WorkflowName { get; init; }

    /// <summary>The version of the definition used.</summary>
    public required int WorkflowVersion { get; init; }

    /// <summary>The business entity this workflow is about.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>The name of the current state in the state machine.</summary>
    public required string CurrentState { get; init; }

    /// <summary>The simplified external status of this instance.</summary>
    public required WorkflowStatus Status { get; init; }

    /// <summary>The actor who started this instance.</summary>
    public required ActorInfo CreatedBy { get; init; }

    /// <summary>The UTC time this instance was started.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>The UTC time this instance last changed state.</summary>
    public required DateTimeOffset ModifiedAt { get; init; }

    /// <summary>
    /// The UTC time this instance reached a terminal state.
    /// Null if the instance is still running.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>
/// A single entry in the immutable audit history of a WorkflowInstance.
/// </summary>
public sealed record WorkflowHistoryEntry
{
    /// <summary>The unique ID of this history entry.</summary>
    public required Guid Id { get; init; }

    /// <summary>The type of event this entry records (e.g., "WorkflowStarted", "ApprovalGranted").</summary>
    public required string EventType { get; init; }

    /// <summary>The state the instance was in before this event. Null for the initial entry.</summary>
    public string? FromState { get; init; }

    /// <summary>The state the instance moved into as a result of this event.</summary>
    public string? ToState { get; init; }

    /// <summary>The step involved in this event, if any.</summary>
    public string? StepName { get; init; }

    /// <summary>The actor who caused this event, if a human triggered it.</summary>
    public ActorInfo? Actor { get; init; }

    /// <summary>An optional comment from the actor (for approval decisions and cancellations).</summary>
    public string? Comment { get; init; }

    /// <summary>The UTC time this event occurred.</summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>
/// A pending approval waiting for a human decision.
/// Returned by <see cref="IApprovalService.GetPendingApprovalsAsync"/>.
/// </summary>
public sealed record PendingApproval
{
    /// <summary>The unique ID of this approval record.</summary>
    public required Guid ApprovalId { get; init; }

    /// <summary>The workflow instance this approval belongs to.</summary>
    public required Guid WorkflowInstanceId { get; init; }

    /// <summary>The workflow definition name (for display purposes).</summary>
    public required string WorkflowName { get; init; }

    /// <summary>The business entity this workflow is about.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>The name of the approval step.</summary>
    public required string StepName { get; init; }

    /// <summary>The actor this approval is currently assigned to.</summary>
    public required ActorInfo AssignedActor { get; init; }

    /// <summary>The UTC time this approval was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The UTC deadline by which a decision must be made.
    /// Null if no escalation policy is configured.
    /// </summary>
    public DateTimeOffset? DeadlineAt { get; init; }
}
