using System;
using System.Threading;
using System.Threading.Tasks;
using Arora.Workflow.Management.Models;

namespace Arora.Workflow.Management;

/// <summary>
/// Service for querying workflow management data.
/// Implementations should optimize for read performance (e.g. AsNoTracking).
/// </summary>
public interface IWorkflowQueryService
{
    Task<PagedResult<WorkflowDefinitionSummary>> GetDefinitionsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
    Task<PagedResult<WorkflowInstanceSummary>> GetInstancesAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    Task<WorkflowInstanceDetails?> GetInstanceDetailsAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<PagedResult<WorkflowHistoryItem>> GetInstanceHistoryAsync(Guid instanceId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}
