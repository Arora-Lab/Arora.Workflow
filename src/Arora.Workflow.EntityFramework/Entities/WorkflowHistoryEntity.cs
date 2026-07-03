namespace Arora.Workflow.EntityFramework.Entities;

/// <summary>
/// The persistence entity for an immutable workflow history entry.
/// Maps to the <c>aw_workflow_history</c> table.
/// </summary>
/// <remarks>
/// History entries are append-only and are never updated or deleted.
/// Every state change and approval decision is recorded here for audit purposes.
/// </remarks>
public sealed class WorkflowHistoryEntity
{
    /// <summary>Unique ID of this history entry.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenant this entry belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The workflow instance this entry belongs to.</summary>
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// The type of event this entry records.
    /// Examples: "WorkflowStarted", "ApprovalGranted", "WorkflowTransitioned".
    /// </summary>
    public string EventType { get; set; } = default!;

    /// <summary>The state the instance was in before this event. Null for the initial entry.</summary>
    public string? FromState { get; set; }

    /// <summary>The state the instance moved into as a result of this event.</summary>
    public string? ToState { get; set; }

    /// <summary>The name of the step involved in this event, if any.</summary>
    public string? StepName { get; set; }

    /// <summary>The ID of the actor who caused this event, if human-triggered.</summary>
    public string? ActorId { get; set; }

    /// <summary>The display name of the actor (denormalized at the time of the event).</summary>
    public string? ActorName { get; set; }

    /// <summary>An optional comment from the actor.</summary>
    public string? Comment { get; set; }

    /// <summary>The UTC time this event occurred.</summary>
    public DateTimeOffset OccurredAt { get; set; }
}
