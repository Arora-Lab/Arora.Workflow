using System.Text.Json;

namespace Arora.Workflow.Internal.Engine.Graph;

/// <summary>
/// Represents the deserialized structure of a workflow definition.
/// Used internally by the engine to evaluate state transitions.
/// </summary>
internal class WorkflowGraph
{
    public string InitialNode { get; set; } = string.Empty;
    public Dictionary<string, WorkflowGraphNode> Nodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Parses a JSON representation of the graph.
    /// </summary>
    public static WorkflowGraph Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return new WorkflowGraph();
        }

        return JsonSerializer.Deserialize<WorkflowGraph>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new WorkflowGraph();
    }
}

public class WorkflowGraphNode
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// For standard nodes, this is "Step" or "Approval".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Assembly-qualified name of the C# type implementing IWorkflowStep, if applicable.
    /// </summary>
    public string? StepType { get; set; }

    public List<WorkflowGraphTransition> Transitions { get; set; } = new();
}

public class WorkflowGraphTransition
{
    public string TargetNode { get; set; } = string.Empty;
    
    /// <summary>
    /// For approvals, this would be "Approved" or "Rejected".
    /// If null, it's an unconditional transition.
    /// </summary>
    public string? Condition { get; set; }
}
