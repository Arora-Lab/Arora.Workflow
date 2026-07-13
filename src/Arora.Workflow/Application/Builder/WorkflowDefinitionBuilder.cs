using System.Text.Json;
using Arora.Workflow.Internal.Engine.Graph;

namespace Arora.Workflow.Application.Builder;

/// <summary>
/// A fluent API builder for constructing workflow definition JSON.
/// </summary>
public class WorkflowDefinitionBuilder
{
    private string? _initialNode;
    private readonly Dictionary<string, WorkflowGraphNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private WorkflowGraphNode? _lastNode;

    /// <summary>
    /// Adds a standard execution step to the workflow.
    /// </summary>
    /// <typeparam name="TStep">The IWorkflowStep implementation.</typeparam>
    /// <param name="stepName">The unique name for this step.</param>
    public WorkflowDefinitionBuilder WithStep<TStep>(string stepName)
    {
        var node = new WorkflowGraphNode 
        {
            Name = stepName,
            Type = "Step",
            StepType = typeof(TStep).AssemblyQualifiedName
        };

        _initialNode ??= stepName;

        _nodes[stepName] = node;
        _lastNode = node;
        
        return this;
    }

    /// <summary>
    /// Adds a manual approval node to the workflow.
    /// </summary>
    /// <param name="stepName">The unique name for this step.</param>
    /// <param name="assignee">The actor ID assigned to this approval.</param>
    public WorkflowDefinitionBuilder WithApproval(string stepName, string? assignee = null)
    {
        var node = new WorkflowGraphNode 
        {
            Name = stepName,
            Type = "Approval",
            Assignee = assignee
        };

        _initialNode ??= stepName;

        _nodes[stepName] = node;
        _lastNode = node;
        
        return this;
    }

    /// <summary>
    /// Defines the transition when an approval is approved.
    /// </summary>
    public WorkflowDefinitionBuilder OnApprove(string nextStepName)
    {
        if (_lastNode?.Type != "Approval")
            throw new InvalidOperationException("OnApprove can only be called immediately after WithApproval.");

        _lastNode.Transitions.Add(new WorkflowGraphTransition 
        {
            TargetNode = nextStepName,
            Condition = "Approved"
        });
        
        return this;
    }

    /// <summary>
    /// Defines the transition when an approval is rejected.
    /// </summary>
    public WorkflowDefinitionBuilder OnReject(string nextStepName)
    {
        if (_lastNode?.Type != "Approval")
            throw new InvalidOperationException("OnReject can only be called immediately after WithApproval.");

        _lastNode.Transitions.Add(new WorkflowGraphTransition 
        {
            TargetNode = nextStepName,
            Condition = "Rejected"
        });
        
        return this;
    }

    /// <summary>
    /// Defines an unconditional transition to the next step.
    /// </summary>
    public WorkflowDefinitionBuilder TransitionsTo(string nextStepName)
    {
        if (_lastNode == null)
            throw new InvalidOperationException("TransitionsTo can only be called after defining a step.");

        _lastNode.Transitions.Add(new WorkflowGraphTransition 
        {
            TargetNode = nextStepName,
            Condition = null
        });
        
        return this;
    }

    /// <summary>
    /// Generates the JSON representation of the workflow graph.
    /// </summary>
    public string BuildJson()
    {
        var graph = new WorkflowGraph
        {
            InitialNode = _initialNode ?? string.Empty,
            Nodes = _nodes
        };
        
        return JsonSerializer.Serialize(graph, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true 
        });
    }
}
