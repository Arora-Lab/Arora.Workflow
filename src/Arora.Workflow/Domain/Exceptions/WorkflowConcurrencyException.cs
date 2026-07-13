using System;

namespace Arora.Workflow.Domain.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency conflict occurs while saving a workflow instance.
/// This typically happens when two processes attempt to mutate the same instance simultaneously.
/// </summary>
public class WorkflowConcurrencyException : Exception
{
    public WorkflowConcurrencyException(string message)
        : base(message)
    {
    }

    public WorkflowConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
