using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Arora.Workflow.Management;
using Arora.Workflow.Management.Models;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.Entities;

namespace Arora.Workflow.EntityFramework.Queries;

internal sealed class EfCoreWorkflowQueryService : IWorkflowQueryService
{
    private readonly DbContext _db;

    public EfCoreWorkflowQueryService(DbContextProvider provider)
    {
        _db = provider.Context;
    }

    public async Task<PagedResult<WorkflowDefinitionSummary>> GetDefinitionsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.Set<WorkflowDefinition>().AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var dbItems = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new {
                x.Id,
                x.Name,
                x.Version,
                x.DefinitionJson,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(x => new WorkflowDefinitionSummary(
                x.Id,
                x.Name,
                x.Version,
                !string.IsNullOrEmpty(x.DefinitionJson) ? JsonSerializer.Deserialize<JsonElement>(x.DefinitionJson, (JsonSerializerOptions?)null).GetProperty("Steps").GetArrayLength() : 0,
                x.CreatedAt)).ToList();

        return new PagedResult<WorkflowDefinitionSummary>(items, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<PagedResult<WorkflowInstanceSummary>> GetInstancesAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.Set<WorkflowInstance>().AsNoTracking();

        if (!string.IsNullOrEmpty(filter.DefinitionId))
        {
            if (Guid.TryParse(filter.DefinitionId, out var definitionIdGuid))
            {
                query = query.Where(x => x.WorkflowDefinitionId == definitionIdGuid);
            }
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            if (Enum.TryParse<Arora.Workflow.Domain.ValueObjects.WorkflowStatus>(filter.Status, true, out var statusEnum))
            {
                query = query.Where(x => x.Status == statusEnum);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new WorkflowInstanceSummary(
                x.Id,
                x.WorkflowDefinitionId,
                x.WorkflowDefinitionVersion,
                x.Status.ToString(),
                x.CurrentState,
                x.CreatedAt,
                x.ModifiedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkflowInstanceSummary>(items, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<WorkflowInstanceDetails?> GetInstanceDetailsAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _db.Set<WorkflowInstance>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == instanceId, cancellationToken);

        if (instance == null) return null;

        return new WorkflowInstanceDetails(
            instance.Id,
            instance.WorkflowDefinitionId,
            instance.WorkflowDefinitionVersion,
            instance.Status.ToString(),
            instance.CurrentState,
            instance.InputJson,
            instance.CreatedAt,
            instance.ModifiedAt);
    }

    public async Task<PagedResult<WorkflowHistoryItem>> GetInstanceHistoryAsync(Guid instanceId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = _db.Set<Arora.Workflow.EntityFramework.Entities.WorkflowHistoryEntity>()
            .AsNoTracking()
            .Where(x => x.WorkflowInstanceId == instanceId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Sequence)
            .ThenBy(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WorkflowHistoryItem(
                x.Id,
                x.WorkflowInstanceId,
                x.StepName,
                x.EventType,
                x.OccurredAt,
                x.ActorName,
                x.Sequence,
                x.NodeId,
                x.FromState,
                x.ToState,
                x.Comment))
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkflowHistoryItem>(items, totalCount, page, pageSize);
    }

    public async Task<WorkflowDefinitionDetails?> GetDefinitionDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var definition = await _db.Set<WorkflowDefinition>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (definition == null) return null;

        var graph = Arora.Workflow.Internal.Engine.Graph.WorkflowGraph.Parse(definition.DefinitionJson);
        
        // 1. Calculate Layout
        var layoutEngine = new Arora.Workflow.Tooling.Layout.LayeredLayoutEngine();
        var layout = layoutEngine.ComputeLayout(graph);

        // 2. Generate Mermaid diagram
        var mermaid = Arora.Workflow.Tooling.Export.MermaidExporter.ToFlowchart(graph);

        // 3. Run diagnostics checks
        var diagnostics = Arora.Workflow.Tooling.Diagnostics.WorkflowDiagnosticsEngine.Analyze(graph);

        return new WorkflowDefinitionDetails(
            definition.Id,
            definition.Name,
            definition.Version,
            definition.Description,
            definition.DefinitionJson,
            definition.CreatedAt,
            layout,
            mermaid,
            diagnostics);
    }
}
