namespace Arora.Workflow.Domain.Exceptions;

/// <summary>
/// Thrown when a workflow step throws an unhandled exception after
/// exhausting all configured retry attempts.
/// </summary>
public sealed class StepExecutionException : WorkflowException
{
    /// <summary>The instance whose step failed.</summary>
    public Guid InstanceId { get; }

    /// <summary>The name of the step that failed.</summary>
    public string StepName { get; }

    /// <summary>The total number of attempts made.</summary>
    public int AttemptCount { get; }

    /// <param name="instanceId">The instance whose step failed.</param>
    /// <param name="stepName">The step that failed.</param>
    /// <param name="attemptCount">How many attempts were made.</param>
    /// <param name="innerException">The last exception thrown by the step.</param>
    public StepExecutionException(
        Guid instanceId,
        string stepName,
        int attemptCount,
        Exception innerException)
        : base(
            $"Step '{stepName}' on workflow instance '{instanceId}' failed " +
            $"after {attemptCount} attempt(s). See InnerException for details.",
            "STEP_EXECUTION_FAILED",
            innerException)
    {
        InstanceId = instanceId;
        StepName = stepName;
        AttemptCount = attemptCount;
    }
}
