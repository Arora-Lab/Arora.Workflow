using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Raised when an actor approves a pending approval, resuming workflow execution.
/// </summary>
/// <param name="WorkflowInstanceId">The instance whose approval was granted.</param>
/// <param name="ApprovalId">The ID of the Approval record that was decided.</param>
/// <param name="DecisionActor">The actor who submitted the approval.</param>
/// <param name="Comment">An optional comment provided by the actor.</param>
/// <param name="OccurredAt">The UTC time the approval was submitted.</param>
public sealed record ApprovalGranted(
    Guid WorkflowInstanceId,
    Guid ApprovalId,
    ActorInfo DecisionActor,
    string? Comment,
    DateTimeOffset OccurredAt) : IWorkflowEvent;

/// <summary>
/// Raised when an actor rejects a pending approval.
/// The workflow transitions to its configured rejection path.
/// </summary>
/// <param name="WorkflowInstanceId">The instance whose approval was rejected.</param>
/// <param name="ApprovalId">The ID of the Approval record that was decided.</param>
/// <param name="DecisionActor">The actor who submitted the rejection.</param>
/// <param name="Comment">An optional comment explaining the rejection.</param>
/// <param name="OccurredAt">The UTC time the rejection was submitted.</param>
public sealed record ApprovalRejected(
    Guid WorkflowInstanceId,
    Guid ApprovalId,
    ActorInfo DecisionActor,
    string? Comment,
    DateTimeOffset OccurredAt) : IWorkflowEvent;
