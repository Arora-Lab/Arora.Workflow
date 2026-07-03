namespace Arora.Workflow.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to mutate a WorkflowInstance that has already
/// reached a terminal state (Completed, Rejected, or Cancelled).
/// Terminal instances are immutable.
/// </summary>
public sealed class WorkflowInTerminalStateException : WorkflowException
{
    /// <summary>The instance that is in a terminal state.</summary>
    public Guid InstanceId { get; }

    /// <summary>The terminal state the instance is in.</summary>
    public string CurrentState { get; }

    /// <param name="instanceId">The instance in a terminal state.</param>
    /// <param name="currentState">The terminal state name.</param>
    public WorkflowInTerminalStateException(Guid instanceId, string currentState)
        : base(
            $"Workflow instance '{instanceId}' is in terminal state '{currentState}' " +
            $"and cannot be modified.",
            "TERMINAL_STATE")
    {
        InstanceId = instanceId;
        CurrentState = currentState;
    }
}

/// <summary>
/// Thrown when a workflow instance is started with an idempotency key
/// that has already been used for another instance.
/// </summary>
/// <remarks>
/// This is not an error in the normal sense — it means the caller already
/// started this workflow successfully. The caller should use
/// <c>IWorkflowService.GetByCorrelationIdAsync</c> to retrieve the existing instance.
/// </remarks>
public sealed class WorkflowAlreadyExistsException : WorkflowException
{
    /// <summary>The idempotency key that was already used.</summary>
    public string IdempotencyKey { get; }

    /// <param name="idempotencyKey">The key that is already in use.</param>
    public WorkflowAlreadyExistsException(string idempotencyKey)
        : base(
            $"A workflow instance already exists for idempotency key '{idempotencyKey}'. " +
            $"Use IWorkflowService.GetByCorrelationIdAsync to retrieve the existing instance.",
            "WORKFLOW_ALREADY_EXISTS")
    {
        IdempotencyKey = idempotencyKey;
    }
}

/// <summary>
/// Thrown when an approval decision is submitted for an Approval that has
/// already been decided (approved, rejected, or escalated).
/// </summary>
public sealed class DuplicateApprovalException : WorkflowException
{
    /// <summary>The ID of the approval that was already decided.</summary>
    public Guid ApprovalId { get; }

    /// <summary>The current status of the approval.</summary>
    public string CurrentStatus { get; }

    /// <param name="approvalId">The approval ID that was already decided.</param>
    /// <param name="currentStatus">The status the approval is already in.</param>
    public DuplicateApprovalException(Guid approvalId, string currentStatus)
        : base(
            $"Approval '{approvalId}' has already been decided. " +
            $"Current status: {currentStatus}.",
            "DUPLICATE_APPROVAL")
    {
        ApprovalId = approvalId;
        CurrentStatus = currentStatus;
    }
}
