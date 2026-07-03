using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// The request object for starting a new workflow instance.
/// </summary>
public sealed record StartWorkflowRequest
{
    /// <summary>
    /// The name of the workflow definition to start.
    /// Must match a Published WorkflowDefinition name exactly.
    /// </summary>
    public required string WorkflowName { get; init; }

    /// <summary>
    /// The specific version of the definition to use.
    /// If null, the latest Published version is used.
    /// </summary>
    public int? Version { get; init; }

    /// <summary>
    /// A caller-provided key that prevents duplicate workflow instances.
    /// If a workflow with this key already exists for this tenant, the existing
    /// instance is returned without creating a new one.
    /// Recommended pattern: use the business entity ID (e.g., <c>$"invoice-{invoiceId}"</c>).
    /// </summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>
    /// A reference to the business entity this workflow is about.
    /// Not interpreted by the engine — passed through to history and events.
    /// Example: an invoice ID, a purchase order number.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// The serializable input payload passed to the first step.
    /// Will be serialized to JSON and stored in the database.
    /// </summary>
    public object? Input { get; init; }

    /// <summary>The actor who is starting this workflow.</summary>
    public required ActorInfo InitiatedBy { get; init; }
}
