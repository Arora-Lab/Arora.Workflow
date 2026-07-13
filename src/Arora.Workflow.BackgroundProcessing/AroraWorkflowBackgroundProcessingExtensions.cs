using System;
using Arora.Workflow.BackgroundProcessing.Configuration;
using Arora.Workflow.BackgroundProcessing.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Arora.Workflow;

public static class AroraWorkflowBackgroundProcessingExtensions
{
    /// <summary>
    /// Registers the background worker to automatically process workflow steps.
    /// </summary>
    public static AroraWorkflowBuilder UseBackgroundProcessing(
        this AroraWorkflowBuilder builder,
        Action<BackgroundProcessingOptions>? configureOptions = null)
    {
        var options = new BackgroundProcessingOptions();
        configureOptions?.Invoke(options);

        // Register the options singleton so the worker can read it
        builder.Services.AddSingleton(options);

        // Register the processor that handles individual items
        builder.Services.AddScoped<WorkItemProcessor>();

        // Register the hosted service that polls the DB
        builder.Services.AddHostedService<WorkflowBackgroundWorker>();

        return builder;
    }
}
