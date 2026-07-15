using System;

namespace Arora.Workflow.Domain.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency conflict occurs while saving a workflow instance.
/// </summary>
public sealed class WorkflowConcurrencyException : WorkflowException
{
    public Guid WorkflowInstanceId { get; }

    public WorkflowConcurrencyException(Guid workflowInstanceId, Exception? innerException = null)
        : base(
            $"Workflow instance '{workflowInstanceId}' was modified by another operation.",
            "WORKFLOW_CONCURRENCY_CONFLICT",
            innerException!)
    {
        WorkflowInstanceId = workflowInstanceId;
    }
}
