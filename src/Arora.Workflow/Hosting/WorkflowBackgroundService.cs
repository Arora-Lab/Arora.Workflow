using System;
using System.Threading;
using System.Threading.Tasks;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Internal.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arora.Workflow.Hosting;

/// <summary>
/// A background service that periodically polls for active workflow instances
/// and invokes the engine to advance them. This enables automated steps, 
/// retries, and timeouts to execute asynchronously without blocking a web request.
/// </summary>
public sealed class WorkflowBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowBackgroundService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(15);

    public WorkflowBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Arora Workflow background service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingWorkflowsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing background workflows.");
            }

            // Wait before next poll cycle
            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Arora Workflow background service is stopping.");
    }

    private async Task ProcessPendingWorkflowsAsync(CancellationToken cancellationToken)
    {
        // 1. Create a dependency injection scope. 
        // This is required because IWorkflowEngine and repositories are Scoped services.
        using var scope = _scopeFactory.CreateScope();
        
        var instanceRepo = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var engine = scope.ServiceProvider.GetRequiredService<IWorkflowEngine>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // 2. Fetch all instances that are currently in the Running status.
        var runningInstances = await instanceRepo.GetRunningInstancesAsync(cancellationToken);

        if (runningInstances.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Found {Count} running workflow instances to process.", runningInstances.Count);

        // 3. Process each instance
        foreach (var instance in runningInstances)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // The engine evaluates the current state and advances it if possible
                await engine.AdvanceAsync(instance, cancellationToken);
                
                // Save the updated instance aggregate (including any newly generated approvals)
                await instanceRepo.UpdateAsync(instance, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to advance workflow instance {InstanceId}.", instance.Id);
                // Optionally, we could set the workflow to a Faulted state here, 
                // but for now we just log the error and let it be retried next cycle.
            }
        }
    }
}
