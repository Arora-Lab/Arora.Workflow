using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Arora.Workflow.EntityFramework.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IApprovalRepository"/>.
/// </summary>
internal sealed class EfCoreApprovalRepository : IApprovalRepository
{
    private readonly DbContext _db;

    public EfCoreApprovalRepository(DbContext db)
    {
        _db = db;
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
        return await _db.Set<Approval>()
            .Where(x => x.WorkflowInstanceId == workflowInstanceId && x.StepName == stepName)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
