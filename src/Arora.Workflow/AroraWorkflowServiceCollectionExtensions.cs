using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Application.Middleware;
using Arora.Workflow.Application.Services;
using Arora.Workflow.Internal.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Arora.Workflow;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register
/// Arora.Workflow core services.
/// </summary>
public static class AroraWorkflowServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Arora.Workflow core services into the DI container.
    /// Chain <c>.UseEntityFramework&lt;TDbContext&gt;()</c> to add persistence.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>
    /// An <see cref="AroraWorkflowBuilder"/> for further configuration chaining.
    /// </returns>
    /// <remarks>
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="IWorkflowService"/> → <c>WorkflowService</c> (Scoped)</item>
    ///   <item><see cref="IApprovalService"/> → <c>ApprovalService</c> (Scoped)</item>
    ///   <item><see cref="IWorkflowEngine"/> → <c>WorkflowEngineStub</c> (Scoped, replaced when engine is complete)</item>
    ///   <item><see cref="IWorkflowClock"/> → <c>SystemClock</c> (Singleton)</item>
    ///   <item>MediatR with Arora.Workflow assembly</item>
    /// </list>
    /// <para>
    /// <see cref="ITenantContext"/> is NOT registered by default — the host application
    /// must register its own implementation based on its identity system.
    /// </para>
    /// </remarks>
    public static AroraWorkflowBuilder AddAroraWorkflow(
        this IServiceCollection services)
    {
        // ── Public services (host applications inject these) ─────────────────
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IApprovalService, ApprovalService>();

        // ── 4. Internal execution engine
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();

        // ── Clock (Singleton — stateless, safe to share) ─────────────────────
        services.AddSingleton<IWorkflowClock, SystemClock>();
        services.AddSingleton<IWorkflowHistoryMetadataSanitizer, DefaultWorkflowHistoryMetadataSanitizer>();

        // ── Step Middleware ──────────────────────────────────────────────────
        // The order of registration dictates execution order:
        // First registered is outer-most.
        // E.g., Logging wraps Retry, which wraps the actual step.
        services.AddScoped<IWorkflowMiddleware, Arora.Workflow.Application.Middleware.LoggingMiddleware>();
        services.AddScoped<IWorkflowMiddleware, Arora.Workflow.Application.Middleware.RetryMiddleware>();

        // ── MediatR (for domain event publication) ───────────────────────────
        // AddMediatR is idempotent — safe to call even if the host app already
        // registered MediatR.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AroraWorkflowBuilder).Assembly));

        return new AroraWorkflowBuilder(services);
    }
}
