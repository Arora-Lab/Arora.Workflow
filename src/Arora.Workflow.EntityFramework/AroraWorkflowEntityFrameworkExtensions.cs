using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Arora.Workflow;

/// <summary>
/// Extension methods on <see cref="AroraWorkflowBuilder"/> for wiring
/// EF Core persistence into Arora.Workflow.
/// </summary>
public static class AroraWorkflowEntityFrameworkExtensions
{
    /// <summary>
    /// Configures Arora.Workflow to use the specified EF Core <c>DbContext</c>
    /// for persistence.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The host application's <c>DbContext</c> type.
    /// Must have <c>builder.ApplyAroraWorkflowMappings()</c> called in its
    /// <c>OnModelCreating</c> override.
    /// </typeparam>
    /// <param name="builder">The Arora.Workflow builder.</param>
    /// <returns>The same <see cref="AroraWorkflowBuilder"/> for further chaining.</returns>
    /// <remarks>
    /// Usage:
    /// <code>
    /// // Program.cs
    /// builder.Services
    ///     .AddAroraWorkflow()
    ///     .UseEntityFramework&lt;AppDbContext&gt;();
    ///
    /// // AppDbContext.cs
    /// protected override void OnModelCreating(ModelBuilder builder)
    /// {
    ///     builder.ApplyAroraWorkflowMappings();
    ///     base.OnModelCreating(builder);
    /// }
    /// </code>
    /// </remarks>
    public static AroraWorkflowBuilder UseEntityFramework<TDbContext>(
        this AroraWorkflowBuilder builder)
        where TDbContext : DbContext
    {
        // Register the concrete DbContext as the base DbContext so the
        // repositories can inject DbContext without knowing the concrete type.
        // This resolves TDbContext from DI and provides it as DbContext.
        builder.Services.AddScoped<DbContext>(
            sp => sp.GetRequiredService<TDbContext>());

        // Register the repository implementations
        builder.Services.AddScoped<IWorkflowDefinitionRepository,
            EfCoreWorkflowDefinitionRepository>();

        builder.Services.AddScoped<IWorkflowInstanceRepository,
            EfCoreWorkflowInstanceRepository>();

        builder.Services.AddScoped<IApprovalRepository,
            EfCoreApprovalRepository>();

        builder.Services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        return builder;
    }
}
