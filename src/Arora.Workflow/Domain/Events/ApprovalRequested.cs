namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Raised when an Approval record is created and a human decision is now required.
/// This is the primary trigger for sending approval notification emails,
/// Teams adaptive cards, Slack messages, etc.
/// </summary>
/// <param name="WorkflowInstanceId">The instance waiting for approval.</param>
/// <param name="ApprovalId">The ID of the Approval record created.</param>
/// <param name="StepName">The name of the approval step.</param>
/// <param name="AssignedActorId">
/// The ID of the actor who must make the decision.
/// Notification handlers use this to look up contact details.
/// </param>
/// <param name="DeadlineAt">
/// The UTC time by which a decision must be made before escalation fires.
/// Null if no escalation policy is configured for this step.
/// </param>
/// <param name="OccurredAt">The UTC time the approval was requested.</param>
public sealed record ApprovalRequested(
    Guid WorkflowInstanceId,
    Guid ApprovalId,
    string StepName,
    string AssignedActorId,
    DateTimeOffset? DeadlineAt,
    DateTimeOffset OccurredAt) : IWorkflowEvent;
