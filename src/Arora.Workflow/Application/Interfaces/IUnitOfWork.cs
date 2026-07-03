namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// Represents a single database transaction boundary.
/// Call <see cref="SaveChangesAsync"/> after mutating aggregates via repositories
/// to commit all changes atomically.
/// </summary>
/// <remarks>
/// Lifetime: Scoped. In the EF Core implementation, this wraps
/// <c>DbContext.SaveChangesAsync()</c>.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending changes in the current scope to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
