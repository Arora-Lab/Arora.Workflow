using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Entities;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Arora.Workflow.EntityFramework.Repositories;

internal sealed class EfCoreWorkflowHistoryRepository : IWorkflowHistoryRepository
{
    private readonly DbContext _db;

    public EfCoreWorkflowHistoryRepository(DbContextProvider provider)
    {
        _db = provider.Context;
    }

    public async Task AddAsync(WorkflowHistory history, CancellationToken cancellationToken = default)
    {
        // Default TenantId for now (could be passed via context)
        var entity = new WorkflowHistoryEntity
        {
            Id = history.Id,
            TenantId = Guid.Empty, // TODO: Tenant resolution
            WorkflowInstanceId = history.WorkflowInstanceId,
            EventType = history.Action,
            ActorId = history.Actor?.Id,
            ActorName = history.Actor?.DisplayName,
            Comment = history.DetailsJson,
            OccurredAt = history.Timestamp
        };
        await _db.Set<WorkflowHistoryEntity>().AddAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowHistory>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var entities = await _db.Set<WorkflowHistoryEntity>()
            .Where(x => x.WorkflowInstanceId == instanceId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync(cancellationToken);

        var list = new List<WorkflowHistory>();
        foreach(var e in entities)
        {
            ActorInfo? actor = null;
            if (e.ActorId != null && e.ActorName != null)
                actor = new ActorInfo(e.ActorId, e.ActorName);

            // Use reflection or deserialization if necessary, but here we just map what we can.
            // Since WorkflowHistory requires action and we have EventType.
            var history = WorkflowHistory.Create(
                e.WorkflowInstanceId,
                e.EventType,
                e.OccurredAt,
                actor,
                e.Comment != null ? JsonDocument.Parse(e.Comment).RootElement : null
            );
            
            // Set the ID using reflection since Create generates a new Guid
            var idProp = typeof(WorkflowHistory).GetProperty("Id");
            idProp?.SetValue(history, e.Id);
            
            list.Add(history);
        }
        return list;
    }
}
