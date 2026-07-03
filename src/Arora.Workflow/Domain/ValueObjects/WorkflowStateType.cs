namespace Arora.Workflow.Domain.ValueObjects;

/// <summary>
/// Classifies the role of a state within a workflow definition's state machine.
/// The engine uses StateType to determine valid transitions and terminal conditions.
/// </summary>
public enum WorkflowStateType
{
    /// <summary>
    /// The starting state. Every workflow definition has exactly one Initial state.
    /// A new WorkflowInstance begins in this state.
    /// </summary>
    Initial,

    /// <summary>
    /// An intermediate state. Execution continues automatically after entering this state.
    /// </summary>
    Intermediate,

    /// <summary>
    /// An intermediate state where execution is paused waiting for a human decision.
    /// The instance stays in this state until <c>IApprovalService.ApproveAsync</c>
    /// or <c>RejectAsync</c> is called.
    /// </summary>
    PendingApproval,

    /// <summary>
    /// A terminal state indicating successful completion of the workflow.
    /// Every workflow definition has exactly one Completed state.
    /// </summary>
    Completed,

    /// <summary>
    /// A terminal state indicating the workflow was rejected at some approval step.
    /// </summary>
    Rejected,

    /// <summary>
    /// A terminal state indicating the workflow was manually cancelled.
    /// </summary>
    Cancelled
}
