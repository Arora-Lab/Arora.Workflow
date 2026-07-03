namespace Arora.Workflow.Domain.ValueObjects;

/// <summary>
/// Represents a named position in the lifecycle of a WorkflowInstance.
/// States are defined by the WorkflowDefinition and are immutable once published.
/// </summary>
/// <remarks>
/// At any moment, a WorkflowInstance is in exactly one state.
/// The engine transitions between states in response to triggers (step completion,
/// approval decision, escalation, cancellation).
/// </remarks>
/// <param name="Name">
/// The unique name of this state within a workflow definition.
/// Used as the persisted value in <c>WorkflowInstance.CurrentState</c>.
/// Example: <c>"PendingManagerApproval"</c>, <c>"Completed"</c>.
/// </param>
/// <param name="StateType">
/// Classifies this state's role in the lifecycle.
/// The engine uses StateType to determine whether execution should pause,
/// continue, or refuse further transitions.
/// </param>
public sealed record WorkflowState(string Name, WorkflowStateType StateType)
{
    /// <summary>
    /// Returns true if this is a terminal state (Completed, Rejected, or Cancelled).
    /// A WorkflowInstance in a terminal state cannot be transitioned further.
    /// </summary>
    public bool IsTerminal =>
        StateType is WorkflowStateType.Completed
            or WorkflowStateType.Rejected
            or WorkflowStateType.Cancelled;

    /// <summary>
    /// Returns true if execution should pause in this state waiting for a human decision.
    /// </summary>
    public bool RequiresApproval => StateType == WorkflowStateType.PendingApproval;

    /// <inheritdoc/>
    public override string ToString() => Name;
}
