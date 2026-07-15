using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.Domain.Events;
using Arora.Workflow.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Arora.Workflow.EntityFramework.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowInstanceRepository"/>.
/// </summary>
internal sealed class EfCoreWorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly DbContext _db;

    public EfCoreWorkflowInstanceRepository(DbContextProvider provider)
    {
        _db = provider.Context;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<WorkflowInstance>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> GetByCorrelationIdAsync(
        string correlationId,
        Guid workflowDefinitionId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<WorkflowInstance>()
            .FirstOrDefaultAsync(
                x => x.CorrelationId == correlationId
                  && x.WorkflowDefinitionId == workflowDefinitionId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<WorkflowInstance>()
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken = default)
    {
        await _db.Set<WorkflowInstance>().AddAsync(instance, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken = default)
    {
        _db.Set<WorkflowInstance>().Update(instance);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowInstance>> GetRunningInstancesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<WorkflowInstance>()
            .Where(x => x.Status == WorkflowStatus.Running)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>.
/// Wraps <c>DbContext.SaveChangesAsync()</c>.
/// </summary>
internal sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly DbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IWorkflowClock _clock;
    private readonly IWorkflowHistoryMetadataSanitizer _sanitizer;

    public EfCoreUnitOfWork(
        DbContextProvider provider,
        ITenantContext tenantContext,
        IWorkflowClock clock,
        IWorkflowHistoryMetadataSanitizer sanitizer)
    {
        _db = provider.Context;
        _tenantContext = tenantContext;
        _clock = clock;
        _sanitizer = sanitizer;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var workflowInstances = _db.ChangeTracker.Entries<WorkflowInstance>()
            .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified)
            .Select(x => x.Entity)
            .ToList();

        // 1. Group by instance to allocate sequence numbers and project history rows
        foreach (var instance in workflowInstances)
        {
            var domainEvents = instance.DomainEvents.ToList();
            if (domainEvents.Count == 0) continue;

            foreach (var domainEvent in domainEvents)
            {
                var sequence = instance.AllocateHistorySequence();
                
                string? stepName = null;
                if (domainEvent is WorkflowTransitioned transitioned)
                    stepName = transitioned.StepName;

                var context = new WorkflowHistoryMetadataContext(
                    instance.TenantId,
                    instance.Id,
                    domainEvent.GetType().Name,
                    stepName);

                var sanitizedMetadata = _sanitizer.Sanitize(domainEvent, context);
                var comment = sanitizedMetadata.HasValue ? JsonSerializer.Serialize(sanitizedMetadata.Value) : null;

                string? actorId = null;
                string? actorName = null;
                string? fromState = null;
                string? toState = null;
                string? nodeId = null;

                switch (domainEvent)
                {
                    case WorkflowStarted started:
                        actorId = started.InitiatedBy?.Id;
                        actorName = started.InitiatedBy?.DisplayName;
                        toState = started.InitialState;
                        nodeId = started.InitialNodeId;
                        break;

                    case WorkflowTransitioned trans:
                        actorId = trans.Actor?.Id;
                        actorName = trans.Actor?.DisplayName;
                        fromState = trans.FromState;
                        toState = trans.ToState;
                        nodeId = trans.ToNodeId;
                        break;

                    case WorkflowCancelled cancelled:
                        actorId = cancelled.CancelledBy?.Id;
                        actorName = cancelled.CancelledBy?.DisplayName;
                        fromState = cancelled.LastActiveState;
                        nodeId = cancelled.CancelledNodeId;
                        break;

                    case WorkflowCompleted completed:
                        toState = "Completed";
                        nodeId = "Completed";
                        break;

                    case WorkflowRejected rejected:
                        actorId = rejected.RejectedBy?.Id;
                        actorName = rejected.RejectedBy?.DisplayName;
                        toState = "Rejected";
                        nodeId = rejected.RejectedAtStep;
                        break;
                }

                var historyEntity = new WorkflowHistoryEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = instance.TenantId,
                    WorkflowInstanceId = instance.Id,
                    EventType = domainEvent.GetType().Name.Replace("Workflow", ""),
                    FromState = fromState,
                    ToState = toState,
                    StepName = stepName,
                    ActorId = actorId,
                    ActorName = actorName,
                    Comment = comment,
                    Sequence = sequence,
                    NodeId = nodeId,
                    OccurredAt = domainEvent.OccurredAt
                };

                _db.Set<WorkflowHistoryEntity>().Add(historyEntity);
            }
        }

        // 2. Update Audit Columns
        UpdateAuditColumns();

        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var firstInstanceId = workflowInstances.FirstOrDefault()?.Id ?? Guid.Empty;
            throw new Arora.Workflow.Domain.Exceptions.WorkflowConcurrencyException(firstInstanceId, ex);
        }
        catch (DbUpdateException ex) when (IsHistorySequenceConflict(ex))
        {
            var firstInstanceId = workflowInstances.FirstOrDefault()?.Id ?? Guid.Empty;
            throw new Arora.Workflow.Domain.Exceptions.WorkflowConcurrencyException(firstInstanceId, ex);
        }
    }

    private bool IsHistorySequenceConflict(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("uq_aw_workflow_history_tenant_instance_sequence", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("23505", StringComparison.OrdinalIgnoreCase) || 
               message.Contains("2627", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateAuditColumns()
    {
        var entries = _db.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var now = _clock.UtcNow;

        foreach (var entry in entries)
        {
            var modifiedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ModifiedAt");
            if (modifiedAtProp != null)
                modifiedAtProp.CurrentValue = now;

            if (entry.State == EntityState.Added)
            {
                var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProp != null && (createdAtProp.CurrentValue == null || (DateTimeOffset)createdAtProp.CurrentValue == default))
                    createdAtProp.CurrentValue = now;
        }
    }
}
}
