namespace Arora.Workflow.EntityFramework.Context;

using System;

/// <summary>
/// Implementing this interface on your <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
/// allows Arora.Workflow to automatically apply Global Query Filters for multi-tenancy.
/// </summary>
public interface IAroraTenantDbContext
{
    /// <summary>
    /// Gets the current TenantId to scope queries.
    /// This should typically be injected into your DbContext and returned here.
    /// </summary>
    Guid TenantId { get; }
}
