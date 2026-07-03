namespace Arora.Workflow.Domain.ValueObjects;

/// <summary>
/// The observable status of a WorkflowInstance.
/// Used for filtering, querying, and display purposes.
/// The engine uses <see cref="WorkflowState"/> internally for transition logic.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>Execution is proceeding through steps automatically.</summary>
    Running,

    /// <summary>Execution is paused, waiting for a human approval decision.</summary>
    PendingApproval,

    /// <summary>The workflow reached its Completed terminal state successfully.</summary>
    Completed,

    /// <summary>The workflow was rejected at an approval step.</summary>
    Rejected,

    /// <summary>The workflow was manually cancelled before completion.</summary>
    Cancelled
}
