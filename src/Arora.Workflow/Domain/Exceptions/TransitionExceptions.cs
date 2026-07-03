namespace Arora.Workflow.Domain.Exceptions;

/// <summary>
/// Thrown when the state machine engine cannot find a valid transition
/// from the current state for the given trigger.
/// This usually indicates the workflow definition is missing a transition
/// for this state/trigger combination.
/// </summary>
public sealed class InvalidTransitionException : WorkflowException
{
    /// <summary>The instance that could not be transitioned.</summary>
    public Guid InstanceId { get; }

    /// <summary>The state the instance was in when the transition was attempted.</summary>
    public string CurrentState { get; }

    /// <summary>A description of the trigger that was applied.</summary>
    public string Trigger { get; }

    /// <param name="instanceId">The instance that could not be transitioned.</param>
    /// <param name="currentState">The current state of the instance.</param>
    /// <param name="trigger">The trigger that was applied.</param>
    public InvalidTransitionException(Guid instanceId, string currentState, string trigger)
        : base(
            $"No valid transition found from state '{currentState}' for trigger '{trigger}' " +
            $"on workflow instance '{instanceId}'.",
            "INVALID_TRANSITION")
    {
        InstanceId = instanceId;
        CurrentState = currentState;
        Trigger = trigger;
    }
}

/// <summary>
/// Thrown when more than one transition matches the current state and trigger.
/// This is a workflow definition authoring error — guards must be mutually exclusive.
/// </summary>
public sealed class AmbiguousTransitionException : WorkflowException
{
    /// <summary>The instance on which the ambiguity was detected.</summary>
    public Guid InstanceId { get; }

    /// <summary>The state the instance was in.</summary>
    public string CurrentState { get; }

    /// <summary>The number of transitions that matched.</summary>
    public int MatchCount { get; }

    /// <param name="instanceId">The instance on which the ambiguity was detected.</param>
    /// <param name="currentState">The current state.</param>
    /// <param name="matchCount">How many transitions matched.</param>
    public AmbiguousTransitionException(Guid instanceId, string currentState, int matchCount)
        : base(
            $"Ambiguous transition: {matchCount} transitions matched from state '{currentState}' " +
            $"on workflow instance '{instanceId}'. " +
            $"Transition guards must be mutually exclusive.",
            "AMBIGUOUS_TRANSITION")
    {
        InstanceId = instanceId;
        CurrentState = currentState;
        MatchCount = matchCount;
    }
}
