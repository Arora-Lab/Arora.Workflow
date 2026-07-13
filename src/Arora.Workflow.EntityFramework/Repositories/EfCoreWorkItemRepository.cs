using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arora.Workflow.EntityFramework.Repositories;

internal sealed class EfCoreWorkItemRepository : IWorkItemRepository
{
    private readonly DbContext _db;

    public EfCoreWorkItemRepository(DbContextProvider provider)
    {
        _db = provider.Context;
    }

    public async Task AddAsync(WorkItem workItem, CancellationToken cancellationToken = default)
    {
        await _db.Set<WorkItem>().AddAsync(workItem, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItem>> ClaimWorkItemsAsync(string workerId, int batchSize, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var lockedUntil = now.Add(leaseDuration);

        // Safe atomic claiming using EF Core 7+ ExecuteUpdateAsync
        // We find items that are Pending AND (AvailableAt <= now)
        // OR items that were processing but their lease expired (LockedUntil < now)
        
        // Due to the way ExecuteUpdate works and the need to return the claimed items, 
        // we can claim them using a subquery or by using raw SQL if needed,
        // but EF Core 7 ExecuteUpdate doesn't natively return the updated rows.
        
        // A standard approach for SQL Server/Postgres without raw SQL:
        // 1. Fetch IDs that are available to claim.
        // 2. Try to update those specific IDs atomically.
        // 3. Fetch the successfully updated rows.
        
        var query = _db.Set<WorkItem>()
            .Where(w => (w.Status == WorkItemStatus.Pending && w.AvailableAt <= now) || 
                        (w.Status == WorkItemStatus.Processing && w.LockedUntil != null && w.LockedUntil < now))
            .OrderBy(w => w.AvailableAt)
            .Take(batchSize);

        var idsToClaim = await query.Select(w => w.Id).ToListAsync(cancellationToken);

        if (!idsToClaim.Any())
        {
            return Array.Empty<WorkItem>();
        }

        // Atomically lock them
        var rowsUpdated = await _db.Set<WorkItem>()
            .Where(w => idsToClaim.Contains(w.Id) && 
                       ((w.Status == WorkItemStatus.Pending && w.AvailableAt <= now) || 
                        (w.Status == WorkItemStatus.Processing && w.LockedUntil != null && w.LockedUntil < now)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.Status, WorkItemStatus.Processing)
                .SetProperty(w => w.LockedBy, workerId)
                .SetProperty(w => w.LockedUntil, lockedUntil)
                .SetProperty(w => w.AttemptCount, w => w.AttemptCount + 1), 
                cancellationToken);

        if (rowsUpdated == 0)
        {
            return Array.Empty<WorkItem>();
        }

        // Retrieve the successfully claimed rows
        return await _db.Set<WorkItem>()
            .Where(w => idsToClaim.Contains(w.Id) && w.LockedBy == workerId && w.LockedUntil == lockedUntil)
            .ToListAsync(cancellationToken);
    }

    public async Task CompleteWorkItemAsync(Guid workItemId, CancellationToken cancellationToken = default)
    {
        await _db.Set<WorkItem>()
            .Where(w => w.Id == workItemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.Status, WorkItemStatus.Completed)
                .SetProperty(w => w.CompletedAt, DateTimeOffset.UtcNow)
                .SetProperty(w => w.LockedBy, (string?)null)
                .SetProperty(w => w.LockedUntil, (DateTimeOffset?)null), 
                cancellationToken);
    }

    public async Task FailWorkItemTransientlyAsync(Guid workItemId, string error, DateTimeOffset nextAvailableAt, CancellationToken cancellationToken = default)
    {
        await _db.Set<WorkItem>()
            .Where(w => w.Id == workItemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.Status, WorkItemStatus.Pending)
                .SetProperty(w => w.LastError, error)
                .SetProperty(w => w.AvailableAt, nextAvailableAt)
                .SetProperty(w => w.LockedBy, (string?)null)
                .SetProperty(w => w.LockedUntil, (DateTimeOffset?)null), 
                cancellationToken);
    }

    public async Task FailWorkItemPermanentlyAsync(Guid workItemId, string error, CancellationToken cancellationToken = default)
    {
        await _db.Set<WorkItem>()
            .Where(w => w.Id == workItemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.Status, WorkItemStatus.DeadLettered)
                .SetProperty(w => w.LastError, error)
                .SetProperty(w => w.LockedBy, (string?)null)
                .SetProperty(w => w.LockedUntil, (DateTimeOffset?)null), 
                cancellationToken);
    }
}
