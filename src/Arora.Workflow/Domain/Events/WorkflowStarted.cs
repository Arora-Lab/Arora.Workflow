using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Raised when a new WorkflowInstance is created and execution has begun.
/// </summary>
/// <param name="WorkflowInstanceId">The ID of the newly created instance.</param>
/// <param name="WorkflowName">The name of the workflow definition that was started.</param>
/// <param name="WorkflowVersion">The version of the definition used.</param>
/// <param name="CorrelationId">
/// The reference to the business entity this workflow is about
/// (e.g., an invoice ID).
/// </param>
/// <param name="InitiatedBy">The actor who started the workflow.</param>
/// <param name="OccurredAt">The UTC time the workflow was started.</param>
public sealed record WorkflowStarted(
    Guid WorkflowInstanceId,
    string WorkflowName,
    int WorkflowVersion,
    string CorrelationId,
    ActorInfo InitiatedBy,
    DateTimeOffset OccurredAt) : IWorkflowEvent;
