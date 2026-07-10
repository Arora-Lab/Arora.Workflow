using System.Text.Json;
using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Entities;

/// <summary>
/// An immutable audit record of an event that occurred during a workflow's execution.
/// </summary>
public sealed class WorkflowHistory
{
    /// <summary>The unique identifier of this history record.</summary>
    public Guid Id { get; private set; }

    /// <summary>The ID of the workflow instance this record belongs to.</summary>
    public Guid WorkflowInstanceId { get; private set; }

    /// <summary>The UTC timestamp when this event occurred.</summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>The type of action that occurred (e.g., "Started", "Transitioned", "Cancelled").</summary>
    public string Action { get; private set; } = default!;

    /// <summary>The user or system that triggered the action.</summary>
    public ActorInfo? Actor { get; private set; }

    /// <summary>Serialized contextual details about the event.</summary>
    public string? DetailsJson { get; private set; }

    private WorkflowHistory() { }

    /// <summary>
    /// Creates a new workflow history record.
    /// </summary>
    public static WorkflowHistory Create(
        Guid workflowInstanceId,
        string action,
        DateTimeOffset timestamp,
        ActorInfo? actor = null,
        object? details = null)
    {
        return new WorkflowHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            Action = action,
            Timestamp = timestamp,
            Actor = actor,
            DetailsJson = details != null ? JsonSerializer.Serialize(details) : null
        };
    }
}
