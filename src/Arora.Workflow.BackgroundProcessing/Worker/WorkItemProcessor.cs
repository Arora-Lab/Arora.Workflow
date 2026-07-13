using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Entities;
using Arora.Workflow.Internal.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arora.Workflow.BackgroundProcessing.Worker;

internal sealed class WorkItemProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkItemProcessor> _logger;

    public WorkItemProcessor(IServiceProvider serviceProvider, ILogger<WorkItemProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing WorkItem {WorkItemId} of type {WorkType} for Instance {InstanceId}", 
            workItem.Id, workItem.WorkType, workItem.WorkflowInstanceId);

        using var scope = _serviceProvider.CreateScope();
        var workItemRepo = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
        
        try
        {
            if (workItem.WorkType == WorkType.ExecuteStep)
            {
                await ExecuteStepAsync(workItem, scope.ServiceProvider, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Unknown WorkType {WorkType} for WorkItem {WorkItemId}", workItem.WorkType, workItem.Id);
            }

            // Mark completed
            await workItemRepo.CompleteWorkItemAsync(workItem.Id, cancellationToken);
            _logger.LogInformation("Successfully completed WorkItem {WorkItemId}", workItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WorkItem {WorkItemId}", workItem.Id);
            
            // For now, retry up to 3 times, then dead-letter.
            if (workItem.AttemptCount >= 3)
            {
                await workItemRepo.FailWorkItemPermanentlyAsync(workItem.Id, ex.ToString(), cancellationToken);
                _logger.LogWarning("WorkItem {WorkItemId} permanently failed after {AttemptCount} attempts.", workItem.Id, workItem.AttemptCount);
            }
            else
            {
                // Exponential backoff
                var backoffSeconds = Math.Pow(2, workItem.AttemptCount) * 10;
                var nextAvailable = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);
                await workItemRepo.FailWorkItemTransientlyAsync(workItem.Id, ex.Message, nextAvailable, cancellationToken);
                _logger.LogInformation("WorkItem {WorkItemId} failed transiently. Next attempt at {NextAvailable}", workItem.Id, nextAvailable);
            }
        }
    }

    private async Task ExecuteStepAsync(WorkItem workItem, IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        var instanceRepo = scopedProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var engine = scopedProvider.GetRequiredService<IWorkflowEngine>();
        var uow = scopedProvider.GetRequiredService<IUnitOfWork>();

        // Set the tenant context so the global query filters and unit of work operate correctly
        var tenantContext = scopedProvider.GetRequiredService<ITenantContext>();
        var prop = tenantContext.GetType().GetProperty("TenantId");
        if (prop != null && prop.CanWrite && Guid.TryParse(workItem.TenantId, out var tenantGuid))
        {
            prop.SetValue(tenantContext, tenantGuid);
        }

        var instance = await instanceRepo.GetByIdAsync(workItem.WorkflowInstanceId, cancellationToken);
        if (instance == null)
            throw new InvalidOperationException($"WorkflowInstance {workItem.WorkflowInstanceId} not found");

        var stepName = "UnknownStep";
        if (!string.IsNullOrEmpty(workItem.Payload))
        {
            var payload = JsonDocument.Parse(workItem.Payload);
            if (payload.RootElement.TryGetProperty("StepName", out var stepNameProp))
            {
                stepName = stepNameProp.GetString() ?? stepName;
            }
        }

        // 1. Execute the step and transition the instance
        await engine.ExecuteStepAsync(instance, stepName, cancellationToken);

        // 2. See if there are any immediate subsequent steps to queue or wait for
        await engine.AdvanceAsync(instance, cancellationToken);

        // 3. Save the instance state (which will also flush domain events via MediatR)
        await uow.SaveChangesAsync(cancellationToken);
    }
}
