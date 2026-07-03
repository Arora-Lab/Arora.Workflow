using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Arora.Workflow.EntityFramework.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowDefinitionRepository"/>.
/// </summary>
internal sealed class EfCoreWorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly DbContext _db;

    public EfCoreWorkflowDefinitionRepository(DbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<WorkflowDefinition>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> GetLatestPublishedAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<WorkflowDefinition>()
            .Where(x => x.Name == name
                     && x.Status == Domain.ValueObjects.WorkflowDefinitionStatus.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> GetByNameAndVersionAsync(
        string name,
        int version,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<WorkflowDefinition>()
            .FirstOrDefaultAsync(
                x => x.Name == name && x.Version == version,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default)
    {
        await _db.Set<WorkflowDefinition>().AddAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default)
    {
        // EF Core change tracking handles updates automatically.
        // This method exists for repository interface consistency and for
        // scenarios where the entity was loaded in a different scope.
        _db.Set<WorkflowDefinition>().Update(definition);
        return Task.CompletedTask;
    }
}
