using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Raised when a deadline elapses without an approval decision and the
/// escalation scheduler promotes the approval to a higher-authority actor.
/// </summary>
/// <param name="WorkflowInstanceId">The instance that was escalated.</param>
/// <param name="ApprovalId">The ID of the Approval record being escalated.</param>
/// <param name="FromActorId">The ID of the original actor who did not respond.</param>
/// <param name="ToActor">The new actor the approval has been reassigned to.</param>
/// <param name="OccurredAt">The UTC time the escalation was processed.</param>
public sealed record WorkflowEscalated(
    Guid WorkflowInstanceId,
    Guid ApprovalId,
    string FromActorId,
    ActorInfo ToActor,
    DateTimeOffset OccurredAt) : IWorkflowEvent;
