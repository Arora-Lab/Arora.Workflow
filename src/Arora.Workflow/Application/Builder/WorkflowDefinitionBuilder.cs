using System.Text.Json;
using Arora.Workflow.Internal.Engine.Graph;

namespace Arora.Workflow.Application.Builder;

/// <summary>
/// A fluent API builder for constructing a workflow definition.
/// </summary>
public class WorkflowDefinitionBuilder
{
    private readonly string _name;
    private string _description = string.Empty;
    private int _version = 1;
    private string? _initialNode;
    private readonly Dictionary<string, WorkflowGraphNode> _nodes = new(StringComparer.OrdinalIgnoreCase);

    private WorkflowDefinitionBuilder(string name)
    {
        _name = name;
    }

    /// <summary>
    /// Starts creating a new workflow definition.
    /// </summary>
    public static WorkflowDefinitionBuilder Create(string name)
    {
        return new WorkflowDefinitionBuilder(name);
    }

    /// <summary>
    /// Sets the description for the workflow definition.
    /// </summary>
    public WorkflowDefinitionBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the version for the workflow definition.
    /// </summary>
    public WorkflowDefinitionBuilder Version(int version)
    {
        _version = version;
        return this;
    }

    /// <summary>
    /// Adds a standard execution step to the workflow.
    /// </summary>
    public IStepBuilder WithStep<TStep>(string stepName)
    {
        var node = new WorkflowGraphNode 
        {
            Name = stepName,
            Type = "Step",
            StepType = typeof(TStep).AssemblyQualifiedName
        };

        _initialNode ??= stepName;
        _nodes[stepName] = node;
        
        return new StepBuilder(this, node);
    }

    /// <summary>
    /// Adds a manual approval node to the workflow.
    /// </summary>
    public IApprovalBuilder WithApproval(string stepName)
    {
        var node = new WorkflowGraphNode 
        {
            Name = stepName,
            Type = "Approval"
        };

        _initialNode ??= stepName;
        _nodes[stepName] = node;
        
        return new ApprovalBuilder(this, node);
    }

    /// <summary>
    /// Generates the WorkflowDefinition object.
    /// </summary>
    public (string Name, int Version, string Description, string Json) Build()
    {
        var graph = new WorkflowGraph
        {
            InitialNode = _initialNode ?? string.Empty,
            Nodes = _nodes
        };
        
        var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true 
        });

        return (_name, _version, _description, json);
    }

    private class StepBuilder : IStepBuilder
    {
        private readonly WorkflowDefinitionBuilder _parent;
        private readonly WorkflowGraphNode _node;

        public StepBuilder(WorkflowDefinitionBuilder parent, WorkflowGraphNode node)
        {
            _parent = parent;
            _node = node;
        }

        public WorkflowDefinitionBuilder TransitionsTo(string nextStepName)
        {
            _node.Transitions.Add(new WorkflowGraphTransition 
            {
                TargetNode = nextStepName,
                Condition = null
            });
            return _parent;
        }

        public WorkflowDefinitionBuilder EndStep()
        {
            return _parent;
        }
    }

    private class ApprovalBuilder : IApprovalBuilder
    {
        private readonly WorkflowDefinitionBuilder _parent;
        private readonly WorkflowGraphNode _node;

        public ApprovalBuilder(WorkflowDefinitionBuilder parent, WorkflowGraphNode node)
        {
            _parent = parent;
            _node = node;
        }

        public IApprovalBuilder AssignedTo(string actorId)
        {
            _node.Assignee = actorId;
            return this;
        }

        public IApprovalBuilder OnApprove(string nextStepName)
        {
            _node.Transitions.Add(new WorkflowGraphTransition 
            {
                TargetNode = nextStepName,
                Condition = "Approved"
            });
            return this;
        }

        public IApprovalBuilder OnReject(string nextStepName)
        {
            _node.Transitions.Add(new WorkflowGraphTransition 
            {
                TargetNode = nextStepName,
                Condition = "Rejected"
            });
            return this;
        }

        public WorkflowDefinitionBuilder EndApproval()
        {
            return _parent;
        }
    }
}
