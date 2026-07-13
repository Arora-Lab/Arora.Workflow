using System;

namespace Arora.Workflow.BackgroundProcessing.Configuration;

/// <summary>
/// Configuration options for the workflow background worker.
/// </summary>
public sealed class BackgroundProcessingOptions
{
    /// <summary>
    /// How often the worker polls the database for new work items.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The maximum number of work items to claim in a single poll.
    /// Default is 10.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// The maximum number of items the worker will process concurrently.
    /// Default is 5.
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 5;

    /// <summary>
    /// The duration for which a claimed work item is locked.
    /// If the item is not completed or failed within this time, it becomes available again.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// A unique identifier for this worker instance (useful for debugging locks).
    /// Defaults to the machine name + GUID.
    /// </summary>
    public string WorkerId { get; set; } = $"{Environment.MachineName}-{Guid.NewGuid():N}";
}
