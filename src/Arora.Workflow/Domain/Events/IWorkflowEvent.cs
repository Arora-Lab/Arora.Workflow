using MediatR;

namespace Arora.Workflow.Domain.Events;

/// <summary>
/// Marker interface for all Arora.Workflow domain events.
/// All domain events implement both this interface and MediatR's
/// <see cref="INotification"/>, which enables in-process dispatch
/// via <c>IMediator.Publish()</c>.
/// </summary>
/// <remarks>
/// Phase 1: Events are dispatched in-process via MediatR after each database commit.
/// Phase 2: The <c>IWorkflowEventPublisher</c> abstraction may be swapped for a
/// durable message broker (Azure Service Bus, RabbitMQ) without changing event schemas.
/// </remarks>
public interface IWorkflowEvent : INotification
{
    /// <summary>The ID of the workflow instance this event belongs to.</summary>
    Guid WorkflowInstanceId { get; }

    /// <summary>The UTC timestamp when this event occurred.</summary>
    DateTimeOffset OccurredAt { get; }
}
