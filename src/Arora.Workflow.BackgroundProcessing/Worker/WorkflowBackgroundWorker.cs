using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.BackgroundProcessing.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arora.Workflow.BackgroundProcessing.Worker;

internal sealed class WorkflowBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundProcessingOptions _options;
    private readonly ILogger<WorkflowBackgroundWorker> _logger;

    public WorkflowBackgroundWorker(
        IServiceProvider serviceProvider,
        BackgroundProcessingOptions options,
        ILogger<WorkflowBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkflowBackgroundWorker started with WorkerId: {WorkerId}", _options.WorkerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred in the background worker loop.");
            }

            // Wait before next poll
            try
            {
                await Task.Delay(_options.PollingInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown
                break;
            }
        }

        _logger.LogInformation("WorkflowBackgroundWorker is stopping.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var workItemRepo = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

        // 1. Claim items atomically
        var claimedItems = await workItemRepo.ClaimWorkItemsAsync(
            _options.WorkerId,
            _options.BatchSize,
            _options.LeaseDuration,
            cancellationToken);

        if (!claimedItems.Any())
        {
            return;
        }

        _logger.LogDebug("Claimed {Count} work items.", claimedItems.Count);

        // 2. Process concurrently
        var processor = scope.ServiceProvider.GetRequiredService<WorkItemProcessor>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxConcurrentJobs,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(claimedItems, parallelOptions, async (workItem, ct) =>
        {
            await processor.ProcessAsync(workItem, ct);
        });
    }
}
