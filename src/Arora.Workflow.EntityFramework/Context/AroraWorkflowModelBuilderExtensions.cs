using Arora.Workflow.EntityFramework.Configurations;
using Arora.Workflow.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arora.Workflow.EntityFramework.Context;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> that apply all Arora.Workflow
/// entity type configurations to the host application's DbContext.
/// </summary>
/// <remarks>
/// Usage inside your <c>AppDbContext.OnModelCreating</c>:
/// <code>
/// protected override void OnModelCreating(ModelBuilder builder)
/// {
///     builder.ApplyAroraWorkflowMappings();
///     base.OnModelCreating(builder);
/// }
/// </code>
/// </remarks>
public static class AroraWorkflowModelBuilderExtensions
{
    /// <summary>
    /// Applies all Arora.Workflow entity configurations to the model.
    /// Call this from your <c>DbContext.OnModelCreating</c> override.
    /// </summary>
    /// <param name="builder">The model builder from your DbContext.</param>
    /// <returns>The same <see cref="ModelBuilder"/> for chaining.</returns>
    public static ModelBuilder ApplyAroraWorkflowMappings(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new WorkflowDefinitionConfiguration());
        builder.ApplyConfiguration(new WorkflowInstanceConfiguration());
        builder.ApplyConfiguration(new ApprovalConfiguration());
        builder.ApplyConfiguration(new WorkflowHistoryConfiguration());

        return builder;
    }
}
