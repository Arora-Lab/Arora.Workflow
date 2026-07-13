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

    public EfCoreUnitOfWork(
        DbContextProvider provider,
        ITenantContext tenantContext,
        IWorkflowClock clock)
    {
        _db = provider.Context;
        _tenantContext = tenantContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var workflowInstances = _db.ChangeTracker.Entries<WorkflowInstance>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = workflowInstances.SelectMany(x => x.DomainEvents).ToList();

        // 1. Update Audit Columns
        UpdateAuditColumns();

        // 2. Project events into History entities attached to the current DbContext.
        // This ensures the audit trail is saved atomically with the state change.
        foreach (var domainEvent in domainEvents)
        {
            // We ensure we only add history entities once per logical operation.
            // If EF Core execution strategy retries SaveChangesAsync, this block runs again,
            // but we can check if it's already added to avoid duplicates.
            // Actually, because we add them to the _db, if they are already in the change tracker
            // as Added, adding them again would either be a no-op or throw. Let's just 
            // check if there's already a history record for this exact event instance by its OccurredAt.
            // Wait, even simpler: just create the entities and check if they are already tracked.
            
            WorkflowHistoryEntity? historyEntity = null;

            switch (domainEvent)
            {
                case WorkflowStarted e:
                    historyEntity = new WorkflowHistoryEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = Guid.Empty, // TenantId is handled by a tenant context typically, but for now we'll set Empty or resolve it.
                        WorkflowInstanceId = e.WorkflowInstanceId,
                        EventType = "Started",
                        ActorId = e.InitiatedBy?.Id,
                        ActorName = e.InitiatedBy?.DisplayName,
                        Comment = JsonSerializer.Serialize(new { e.WorkflowName, e.CorrelationId }),
                        OccurredAt = e.OccurredAt
                    };
                    break;
                case WorkflowTransitioned e:
                    historyEntity = new WorkflowHistoryEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = Guid.Empty,
                        WorkflowInstanceId = e.WorkflowInstanceId,
                        EventType = "Transitioned",
                        ActorId = e.Actor?.Id,
                        ActorName = e.Actor?.DisplayName,
                        FromState = e.FromState,
                        ToState = e.ToState,
                        StepName = e.StepName,
                        OccurredAt = e.OccurredAt
                    };
                    break;
                case WorkflowCancelled e:
                    historyEntity = new WorkflowHistoryEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = Guid.Empty,
                        WorkflowInstanceId = e.WorkflowInstanceId,
                        EventType = "Cancelled",
                        ActorId = e.CancelledBy?.Id,
                        ActorName = e.CancelledBy?.DisplayName,
                        Comment = JsonSerializer.Serialize(new { e.Reason }),
                        OccurredAt = e.OccurredAt
                    };
                    break;
            }

            if (historyEntity != null)
            {
                // Prevent duplicate tracking if EF retries this SaveChangesAsync call.
                bool alreadyTracked = _db.ChangeTracker.Entries<WorkflowHistoryEntity>()
                    .Any(x => x.Entity.WorkflowInstanceId == historyEntity.WorkflowInstanceId &&
                              x.Entity.OccurredAt == historyEntity.OccurredAt &&
                              x.Entity.EventType == historyEntity.EventType);

                if (!alreadyTracked)
                {
                    _db.Set<WorkflowHistoryEntity>().Add(historyEntity);
                }
            }
        }

        // We DO NOT clear the domain events here.
        // WorkflowService will read them to publish MediatR notifications AFTER this commit succeeds.
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new Arora.Workflow.Domain.Exceptions.WorkflowConcurrencyException(
                "A concurrency conflict occurred while saving the workflow instance. " +
                "The instance was modified by another process.", ex);
        }
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
