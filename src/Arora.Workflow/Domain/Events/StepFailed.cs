namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Raised when a workflow step exhausts all retry attempts and fails permanently.
/// After this event, the workflow engine will not retry the step.
/// The instance transitions to a failed state or triggers a compensating action,
/// depending on the workflow definition's error handling configuration.
/// </summary>
/// <param name="WorkflowInstanceId">The instance the step belongs to.</param>
/// <param name="StepName">The name of the step that failed.</param>
/// <param name="AttemptNumber">The total number of attempts made before giving up.</param>
/// <param name="ErrorMessage">A summary of the last exception message.</param>
/// <param name="OccurredAt">The UTC time the final failure was recorded.</param>
public sealed record StepFailed(
    Guid WorkflowInstanceId,
    string StepName,
    int AttemptNumber,
    string ErrorMessage,
    DateTimeOffset OccurredAt) : IWorkflowEvent;
