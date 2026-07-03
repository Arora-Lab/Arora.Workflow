namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Raised when a workflow step completes successfully.
/// </summary>
/// <param name="WorkflowInstanceId">The instance the step belongs to.</param>
/// <param name="StepName">The name of the step that completed.</param>
/// <param name="AttemptNumber">
/// Which attempt this was. 1 means it succeeded on the first try.
/// Greater than 1 means it succeeded after retries.
/// </param>
/// <param name="DurationMs">Wall-clock execution time in milliseconds.</param>
/// <param name="OccurredAt">The UTC time the step completed.</param>
public sealed record StepExecuted(
    Guid WorkflowInstanceId,
    string StepName,
    int AttemptNumber,
    int DurationMs,
    DateTimeOffset OccurredAt) : IWorkflowEvent;
