using Microsoft.Extensions.DependencyInjection;

namespace Arora.Workflow;

/// <summary>
/// A builder returned by <see cref="AroraWorkflowServiceCollectionExtensions.AddAroraWorkflow"/>
/// that enables chained configuration of Arora.Workflow services.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// services.AddAroraWorkflow()
///         .UseEntityFramework&lt;AppDbContext&gt;();
/// </code>
/// </remarks>
public sealed class AroraWorkflowBuilder
{
    /// <summary>
    /// The underlying service collection.
    /// Available for advanced customization — e.g., replacing a default
    /// implementation with a custom one.
    /// </summary>
    public IServiceCollection Services { get; }

    internal AroraWorkflowBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Registers the Arora Workflow background service to periodically poll
    /// and advance active workflow instances.
    /// </summary>
    public AroraWorkflowBuilder AddBackgroundWorker()
    {
        Services.AddHostedService<Hosting.WorkflowBackgroundService>();
        return this;
    }
}
