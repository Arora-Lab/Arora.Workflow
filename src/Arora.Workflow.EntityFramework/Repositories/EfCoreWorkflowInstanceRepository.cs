using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

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

    public EfCoreUnitOfWork(DbContextProvider provider)
    {
        _db = provider.Context;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
