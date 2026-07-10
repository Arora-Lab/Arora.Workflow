using Arora.Workflow.Domain.Entities;

namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// Repository for persisting and retrieving workflow history records.
/// </summary>
public interface IWorkflowHistoryRepository
{
    /// <summary>
    /// Appends a new history record.
    /// </summary>
    Task AddAsync(WorkflowHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all history records for a specific workflow instance, ordered by timestamp ascending.
    /// </summary>
    Task<IReadOnlyList<WorkflowHistory>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default);
}
