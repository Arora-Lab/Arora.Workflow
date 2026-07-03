namespace Arora.Workflow.Domain.ValueObjects;

/// <summary>
/// Represents the identity of a person or system that performed an action
/// on a workflow instance (e.g., started it, approved it, cancelled it).
/// </summary>
/// <remarks>
/// ActorInfo is a value object — it has no identity of its own beyond its properties.
/// Arora.Workflow stores actor information but does not manage identity or
/// permissions. The host application is responsible for resolving actor identity
/// and passing it to the workflow services.
/// </remarks>
/// <param name="Id">
/// The unique identifier of the actor in the host application's identity system.
/// Typically a user ID, service account ID, or system identifier.
/// </param>
/// <param name="DisplayName">
/// A human-readable name for the actor, used in audit history and notifications.
/// Denormalized at the time of action so history remains accurate even if the
/// actor's display name changes later.
/// </param>
public sealed record ActorInfo(string Id, string DisplayName)
{
    /// <summary>
    /// Represents an automated system action with no specific human actor.
    /// Used internally by the engine for escalations and timeouts.
    /// </summary>
    public static readonly ActorInfo System = new("system", "System");
}
