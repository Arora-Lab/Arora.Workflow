using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Arora.Workflow.EntityFramework.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IApprovalRepository"/>.
/// </summary>
internal sealed class EfCoreApprovalRepository : IApprovalRepository
{
    private readonly DbContext _db;

    public EfCoreApprovalRepository(DbContextProvider provider)
    {
        _db = provider.Context;
    }

    /// <inheritdoc />
    public async Task<Approval?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<Approval>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        Approval approval,
        CancellationToken cancellationToken = default)
    {
        await _db.Set<Approval>().AddAsync(approval, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(
        Approval approval,
        CancellationToken cancellationToken = default)
    {
        _db.Set<Approval>().Update(approval);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<Approval?> GetLatestApprovalAsync(
        Guid workflowInstanceId,
        string stepName,
        CancellationToken cancellationToken = default)
    {
        var approvals = await _db.Set<Approval>()
            .Where(a => a.WorkflowInstanceId == workflowInstanceId && a.StepName == stepName)
            .ToListAsync(cancellationToken);

        return approvals
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Approval>> GetPendingByActorAsync(
        ActorInfo actor,
        CancellationToken cancellationToken = default)
    {
        var approvals = await _db.Set<Approval>()
            .Where(a => a.Status == ApprovalStatus.Pending && a.AssignedActor.Id == actor.Id)
            .ToListAsync(cancellationToken);

        return approvals
            .OrderBy(a => a.CreatedAt)
            .ToList();
    }
}
