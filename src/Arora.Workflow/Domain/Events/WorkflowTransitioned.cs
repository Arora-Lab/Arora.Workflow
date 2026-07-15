using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Raised when a WorkflowInstance moves from one state to another.
/// This is the core event of the state machine — every transition produces one.
/// </summary>
/// <param name="WorkflowInstanceId">The instance that transitioned.</param>
/// <param name="FromState">The state the instance was in before the transition.</param>
/// <param name="ToState">The state the instance moved into.</param>
/// <param name="StepName">
/// The step whose completion triggered this transition, if applicable.
/// Null for transitions triggered by approval decisions or cancellation.
/// </param>
/// <param name="Actor">
/// The actor who caused the transition, if a human triggered it.
/// Null for automatic step-completion transitions.
/// </param>
/// <param name="OccurredAt">The UTC time of the transition.</param>
public sealed record WorkflowTransitioned(
    Guid WorkflowInstanceId,
    string FromState,
    string ToState,
    string FromNodeId,
    string ToNodeId,
    string? StepName,
    ActorInfo? Actor,
    DateTimeOffset OccurredAt) : IWorkflowEvent;
