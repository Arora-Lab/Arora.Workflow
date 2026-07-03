using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.Exceptions;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.Internal.Engine.Graph;
using System.Text.Json;

namespace Arora.Workflow.Internal.Engine;

/// <summary>
/// The core execution engine for workflow instances.
/// Responsible for evaluating state transitions based on the workflow definition.
/// </summary>
internal sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowDefinitionRepository _definitionRepo;
    private readonly IApprovalRepository _approvalRepo;
    private readonly IWorkflowClock _clock;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowEngine(
        IWorkflowDefinitionRepository definitionRepo,
        IApprovalRepository approvalRepo,
        IWorkflowClock clock,
        IServiceProvider serviceProvider)
    {
        _definitionRepo = definitionRepo ?? throw new ArgumentNullException(nameof(definitionRepo));
        _approvalRepo = approvalRepo ?? throw new ArgumentNullException(nameof(approvalRepo));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task AdvanceAsync(
        WorkflowInstance instance,
        CancellationToken cancellationToken = default)
    {
        if (instance.IsInTerminalState())
            return;

        // 1. Fetch Definition
        var definition = await _definitionRepo.GetByIdAsync(instance.WorkflowDefinitionId, cancellationToken);
        if (definition == null)
            throw new InvalidOperationException($"Definition {instance.WorkflowDefinitionId} not found for instance {instance.Id}");

        // 2. Parse Graph
        var graph = WorkflowGraph.Parse(definition.DefinitionJson);
        
        bool canAdvance = true;

        while (canAdvance && !instance.IsInTerminalState())
        {
            var currentStateName = instance.CurrentState;
            
            // If the graph is entirely empty, we just mark it completed. 
            // For real implementations, a proper validation occurs during definition creation.
            if (!graph.Nodes.TryGetValue(currentStateName, out var node))
            {
                instance.TransitionTo(
                    new WorkflowState("Completed", WorkflowStateType.Completed),
                    null,
                    _clock.UtcNow);
                break;
            }

            // Execute node logic based on type
            if (node.Type.Equals("Approval", StringComparison.OrdinalIgnoreCase))
            {
                // We are at an approval node. Check if there's a decided approval.
                var approval = await _approvalRepo.GetLatestApprovalAsync(instance.Id, currentStateName, cancellationToken);
                
                if (approval != null && approval.Status != ApprovalStatus.Pending)
                {
                    // Find the matching transition for this decision
                    var decisionCondition = approval.Status.ToString(); // "Approved" or "Rejected"
                    
                    var matchingTransition = node.Transitions.FirstOrDefault(t => 
                        string.Equals(t.Condition, decisionCondition, StringComparison.OrdinalIgnoreCase))
                        ?? node.Transitions.FirstOrDefault(t => string.IsNullOrEmpty(t.Condition));

                    if (matchingTransition != null)
                    {
                        instance.TransitionTo(
                            new WorkflowState(matchingTransition.TargetNode, WorkflowStateType.Intermediate),
                            null,
                            _clock.UtcNow);
                    }
                    else
                    {
                        // No matching transition and no default transition
                        instance.TransitionTo(
                            new WorkflowState("Completed", WorkflowStateType.Completed),
                            null,
                            _clock.UtcNow);
                    }
                }
                else
                {
                    // No decision yet, we wait.
                    canAdvance = false;
                }
            }
            else
            {
                // It's a standard Step
                var stepResultCondition = (string?)null;

                if (!string.IsNullOrEmpty(node.StepType))
                {
                    var stepType = Type.GetType(node.StepType);
                    if (stepType == null) throw new InvalidOperationException($"Type.GetType returned null for {node.StepType}");
                    
                    // Create a scope for this step execution
                    using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(_serviceProvider);
                    
                    var stepInstance = scope.ServiceProvider.GetService(stepType) ?? Activator.CreateInstance(stepType);
                    if (stepInstance is Arora.Workflow.Application.Steps.IWorkflowStep step)
                    {
                        var context = new Arora.Workflow.Application.Steps.StepExecutionContext
                        {
                            Instance = instance,
                            StepName = currentStateName,
                            CancellationToken = cancellationToken
                        };
                        // Construct middleware pipeline
                        var middlewares = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                            .GetServices<Arora.Workflow.Application.Middleware.IWorkflowMiddleware>(scope.ServiceProvider)
                            .Reverse()
                            .ToList();
                        
                        Arora.Workflow.Application.Middleware.WorkflowStepDelegate pipeline = (ctx) => step.ExecuteAsync(ctx);
                        
                        foreach (var middleware in middlewares)
                        {
                            var next = pipeline;
                            pipeline = (ctx) => middleware.InvokeAsync(ctx, next);
                        }

                        stepResultCondition = await pipeline(context);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Step type '{node.StepType}' does not implement IWorkflowStep.");
                    }
                }

                var nextTransition = node.Transitions.FirstOrDefault(t => 
                    string.Equals(t.Condition, stepResultCondition, StringComparison.OrdinalIgnoreCase))
                    ?? node.Transitions.FirstOrDefault(t => string.IsNullOrEmpty(t.Condition));

                if (nextTransition != null)
                {
                    instance.TransitionTo(
                        new WorkflowState(nextTransition.TargetNode, WorkflowStateType.Intermediate),
                        null,
                        _clock.UtcNow);
                }
                else
                {
                    instance.TransitionTo(
                        new WorkflowState("Completed", WorkflowStateType.Completed),
                        null,
                        _clock.UtcNow);
                }
            }
        }
    }
}
