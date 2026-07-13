using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// Repository for reading and persisting Approval aggregates.
/// </summary>
public interface IApprovalRepository
{
    /// <summary>
    /// Returns an approval by its unique ID, or null if not found.
    /// </summary>
    Task<Approval?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new Approval to the database.
    /// </summary>
    Task AddAsync(
        Approval approval,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to an existing Approval.
    /// </summary>
    Task UpdateAsync(
        Approval approval,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest approval for a given instance and step.
    /// </summary>
    Task<Approval?> GetLatestApprovalAsync(
        Guid workflowInstanceId,
        string stepName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending approvals assigned to a specific actor.
    /// </summary>
    Task<IReadOnlyList<Approval>> GetPendingByActorAsync(
        ActorInfo actor,
        CancellationToken cancellationToken = default);
}
