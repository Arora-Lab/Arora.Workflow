using Arora.Workflow.Domain.Events;
using Arora.Workflow.Domain.Exceptions;
using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Aggregates;

/// <summary>
/// The aggregate root for a single execution of a workflow.
/// This is the most important class in the Arora.Workflow domain.
///
/// Responsibilities:
/// <list type="bullet">
///   <item>Holding the current execution state</item>
///   <item>Enforcing all invariants (terminal state, pending approvals, etc.)</item>
///   <item>Collecting domain events to be published after the database commit</item>
///   <item>Recording immutable history entries</item>
/// </list>
///
/// The engine mutates this aggregate; repositories persist it.
/// The aggregate never calls the database or publishes events itself.
/// </summary>
public sealed class WorkflowInstance
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    /// <summary>The unique identifier of this workflow instance.</summary>
    public Guid Id { get; private set; }

    /// <summary>The ID of the tenant this instance belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The ID of the workflow definition this instance is executing.</summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>The version of the workflow definition at the time this instance was started.</summary>
    public int WorkflowDefinitionVersion { get; private set; }

    /// <summary>The name of the workflow definition (denormalized for display).</summary>
    public string WorkflowName { get; private set; } = default!;

    /// <summary>
    /// A reference to the business entity this workflow is about.
    /// Provided by the host application. Not interpreted by the engine.
    /// Example: an invoice ID, a purchase order number.
    /// </summary>
    public string CorrelationId { get; private set; } = default!;

    /// <summary>
    /// A caller-provided key that prevents duplicate instances.
    /// The engine enforces uniqueness of IdempotencyKey per tenant.
    /// </summary>
    public string IdempotencyKey { get; private set; } = default!;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    /// <summary>
    /// The name of the state this instance is currently in.
    /// Persisted to the database as a string. Used by the engine for
    /// transition evaluation.
    /// </summary>
    public string CurrentState { get; private set; } = default!;

    /// <summary>The simplified external status of this instance.</summary>
    public WorkflowStatus Status { get; private set; }

    /// <summary>
    /// The serialized input provided when the workflow was started.
    /// Stored for reference; the engine passes this to the first step.
    /// </summary>
    public string? InputJson { get; private set; }

    // -------------------------------------------------------------------------
    // Timestamps
    // -------------------------------------------------------------------------

    /// <summary>The UTC time this instance was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The actor who started this instance.</summary>
    public ActorInfo CreatedBy { get; private set; } = default!;

    /// <summary>The UTC time this instance last changed state.</summary>
    public DateTimeOffset ModifiedAt { get; private set; }

    /// <summary>The UTC time this instance reached a terminal state. Null if still running.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Whether this instance is soft deleted.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>When this instance was soft deleted.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    // -------------------------------------------------------------------------
    // Domain events — collected, not published. The engine reads and clears
    // this list after the database commit. Handlers are invoked outside the
    // aggregate.
    // -------------------------------------------------------------------------

    private readonly List<IWorkflowEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised during this operation.
    /// Read by the engine after committing to the database, then cleared.
    /// </summary>
    public IReadOnlyList<IWorkflowEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Clears collected domain events after they have been published.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    // -------------------------------------------------------------------------
    // Constructor — private, use factory methods
    // -------------------------------------------------------------------------

    private WorkflowInstance() { }

    // -------------------------------------------------------------------------
    // Factory method
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates and starts a new WorkflowInstance.
    /// Raises <see cref="WorkflowStarted"/>.
    /// </summary>
    /// <param name="tenantId">The tenant this instance belongs to.</param>
    /// <param name="definitionId">The workflow definition to execute.</param>
    /// <param name="definitionVersion">The version of the definition.</param>
    /// <param name="workflowName">The definition name (denormalized for display).</param>
    /// <param name="correlationId">The business entity reference.</param>
    /// <param name="idempotencyKey">A unique key preventing duplicate starts.</param>
    /// <param name="initialState">The initial state from the definition.</param>
    /// <param name="inputJson">The serialized input for the first step.</param>
    /// <param name="initiatedBy">The actor starting the workflow.</param>
    /// <param name="clock">The current UTC time.</param>
    /// <returns>A new, started WorkflowInstance.</returns>
    public static WorkflowInstance Start(
        Guid tenantId,
        Guid definitionId,
        int definitionVersion,
        string workflowName,
        string correlationId,
        string idempotencyKey,
        WorkflowState initialState,
        string? inputJson,
        ActorInfo initiatedBy,
        DateTimeOffset clock)
    {
        var instance = new WorkflowInstance
        {
            Id                       = Guid.NewGuid(),
            TenantId                 = tenantId,
            WorkflowDefinitionId     = definitionId,
            WorkflowDefinitionVersion = definitionVersion,
            WorkflowName             = workflowName,
            CorrelationId            = correlationId,
            IdempotencyKey           = idempotencyKey,
            CurrentState             = initialState.Name,
            Status                   = WorkflowStatus.Running,
            InputJson                = inputJson,
            CreatedAt                = clock,
            CreatedBy                = initiatedBy,
            ModifiedAt               = clock,
            IsDeleted                = false
        };

        instance._domainEvents.Add(new WorkflowStarted(
            instance.Id,
            workflowName,
            definitionVersion,
            correlationId,
            initiatedBy,
            clock));

        return instance;
    }

    // -------------------------------------------------------------------------
    // Behaviours
    // -------------------------------------------------------------------------

    /// <summary>
    /// Transitions this instance to a new state.
    /// </summary>
    /// <param name="toState">The destination state.</param>
    /// <param name="trigger">A description of what triggered this transition.</param>
    /// <param name="actor">The actor who caused the transition, if human-triggered.</param>
    /// <param name="stepName">The step that completed, if step-triggered.</param>
    /// <param name="clock">The current UTC time.</param>
    /// <exception cref="WorkflowInTerminalStateException">
    /// Thrown if this instance is already in a terminal state.
    /// </exception>
    public void TransitionTo(
        WorkflowState toState,
        string trigger,
        DateTimeOffset clock,
        ActorInfo? actor = null,
        string? stepName = null)
    {
        // INVARIANT: terminal instances cannot transition further.
        // This is the single enforcement point — nowhere else needs to check this.
        if (IsInTerminalState())
            throw new WorkflowInTerminalStateException(Id, CurrentState);

        var fromState = CurrentState;
        CurrentState = toState.Name;
        ModifiedAt   = clock;

        // Map the internal StateType to the external WorkflowStatus
        Status = toState.StateType switch
        {
            WorkflowStateType.PendingApproval => WorkflowStatus.PendingApproval,
            WorkflowStateType.Completed       => WorkflowStatus.Completed,
            WorkflowStateType.Rejected        => WorkflowStatus.Rejected,
            WorkflowStateType.Cancelled       => WorkflowStatus.Cancelled,
            _                                 => WorkflowStatus.Running
        };

        if (toState.IsTerminal)
            CompletedAt = clock;

        _domainEvents.Add(new WorkflowTransitioned(
            Id, fromState, toState.Name, stepName, actor, clock));
    }

    /// <summary>
    /// Cancels this instance, regardless of its current state.
    /// If already in a terminal state, this is a no-op (idempotent).
    /// </summary>
    /// <param name="reason">A human-readable reason for cancellation.</param>
    /// <param name="cancelledBy">The actor initiating the cancellation.</param>
    /// <param name="clock">The current UTC time.</param>
    public void Cancel(string reason, ActorInfo cancelledBy, DateTimeOffset clock)
    {
        // Cancellation is idempotent — if already terminal, do nothing.
        if (IsInTerminalState()) return;

        var fromState = CurrentState;
        CurrentState = "Cancelled";
        Status       = WorkflowStatus.Cancelled;
        CompletedAt  = clock;
        ModifiedAt   = clock;

        _domainEvents.Add(new WorkflowTransitioned(
            Id, fromState, "Cancelled", null, cancelledBy, clock));

        _domainEvents.Add(new WorkflowCancelled(
            Id, WorkflowName, CorrelationId, reason, cancelledBy, clock));
    }

    // -------------------------------------------------------------------------
    // Queries (read-only checks, no state mutation)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns true if this instance is in a terminal state and cannot be
    /// transitioned further.
    /// </summary>
    public bool IsInTerminalState() =>
        Status is WorkflowStatus.Completed
            or WorkflowStatus.Rejected
            or WorkflowStatus.Cancelled;

    /// <summary>Returns true if this instance is waiting for a human decision.</summary>
    public bool IsPendingApproval() => Status == WorkflowStatus.PendingApproval;

    /// <inheritdoc/>
    public override string ToString() =>
        $"WorkflowInstance [{Id}] '{WorkflowName}' | State: {CurrentState} | Status: {Status}";
}
