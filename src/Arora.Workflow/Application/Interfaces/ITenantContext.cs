namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// Provides the current tenant identity for scoped workflow operations.
/// </summary>
/// <remarks>
/// Implementations typically resolve the tenant from the current HTTP context,
/// a JWT claim, or an ambient DI scope value set by the host application.
/// Lifetime: Scoped.
/// </remarks>
public interface ITenantContext
{
    /// <summary>
    /// The ID of the tenant for the current request scope.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no tenant has been established for the current scope.
    /// This indicates a misconfigured host application — all workflow operations
    /// must run within an authenticated, tenant-resolved scope.
    /// </exception>
    Guid TenantId { get; }
}
