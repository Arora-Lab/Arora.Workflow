using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arora.Workflow.Domain.Entities;

namespace Arora.Workflow.Application.Interfaces;

public interface IWorkItemRepository
{
    Task AddAsync(WorkItem workItem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkItem>> ClaimWorkItemsAsync(string workerId, int batchSize, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task CompleteWorkItemAsync(Guid workItemId, CancellationToken cancellationToken = default);
    Task FailWorkItemTransientlyAsync(Guid workItemId, string error, DateTimeOffset nextAvailableAt, CancellationToken cancellationToken = default);
    Task FailWorkItemPermanentlyAsync(Guid workItemId, string error, CancellationToken cancellationToken = default);
}
