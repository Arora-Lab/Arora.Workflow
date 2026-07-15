using System;
using System.Text;
using Arora.Workflow.Internal.Engine.Graph;

namespace Arora.Workflow.Tooling.Export;

/// <summary>
/// Exporter to generate Mermaid diagram text representations from a workflow graph.
/// </summary>
public static class MermaidExporter
{
    /// <summary>
    /// Generates a Mermaid flowchart (graph TD) from the provided workflow graph definition.
    /// </summary>
    public static string ToFlowchart(WorkflowGraph graph)
    {
        if (graph?.Nodes == null || graph.Nodes.Count == 0)
        {
            return "graph TD\n    Empty[No nodes defined]";
        }

        var sb = new StringBuilder();
        sb.AppendLine("graph TD");

        // 1. Declare nodes with their shapes
        foreach (var nodeKv in graph.Nodes)
        {
            var node = nodeKv.Value;
            var name = nodeKv.Key;
            
            // Format name securely to prevent breaking mermaid syntax
            var sanitizedName = SanitizeForMermaidId(name);
            var sanitizedLabel = SanitizeForMermaidLabel(node.Name);

            if (string.Equals(node.Type, "Approval", StringComparison.OrdinalIgnoreCase))
            {
                // Approval is rendered as a decision diamond shape
                sb.AppendLine($"    {sanitizedName}{{\"{sanitizedLabel}\"}}");
            }
            else
            {
                // Step is rendered as a standard rectangle box
                sb.AppendLine($"    {sanitizedName}[\"{sanitizedLabel}\"]");
            }
        }

        // Highlight initial/start node in a custom style
        if (!string.IsNullOrEmpty(graph.InitialNode))
        {
            var initialSanitized = SanitizeForMermaidId(graph.InitialNode);
            sb.AppendLine($"    style {initialSanitized} fill:#3b82f6,stroke:#1d4ed8,stroke-width:2px,color:#fff");
        }

        sb.AppendLine();

        // 2. Declare transitions
        foreach (var nodeKv in graph.Nodes)
        {
            var sourceName = SanitizeForMermaidId(nodeKv.Key);
            var node = nodeKv.Value;

            foreach (var transition in node.Transitions)
            {
                if (graph.Nodes.ContainsKey(transition.TargetNode))
                {
                    var targetName = SanitizeForMermaidId(transition.TargetNode);
                    if (string.IsNullOrWhiteSpace(transition.Condition))
                    {
                        sb.AppendLine($"    {sourceName} --> {targetName}");
                    }
                    else
                    {
                        var sanitizedCondition = SanitizeForMermaidLabel(transition.Condition);
                        sb.AppendLine($"    {sourceName} -->|{sanitizedCondition}| {targetName}");
                    }
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a Mermaid sequence diagram showing the workflow actors and engine paths.
    /// </summary>
    public static string ToSequenceDiagram(WorkflowGraph graph)
    {
        if (graph?.Nodes == null || graph.Nodes.Count == 0)
        {
            return "sequenceDiagram\n    participant Empty as No nodes";
        }

        var sb = new StringBuilder();
        sb.AppendLine("sequenceDiagram");
        sb.AppendLine("    autonumber");
        sb.AppendLine("    actor Dev as Client/User");
        sb.AppendLine("    participant Engine as Workflow Engine");

        // List out all steps as participants
        foreach (var nodeKv in graph.Nodes)
        {
            var name = SanitizeForMermaidLabel(nodeKv.Value.Name);
            sb.AppendLine($"    participant {SanitizeForMermaidId(nodeKv.Key)} as {name}");
        }

        sb.AppendLine();

        // Trace paths starting from InitialNode
        if (!string.IsNullOrEmpty(graph.InitialNode) && graph.Nodes.ContainsKey(graph.InitialNode))
        {
            var firstNodeName = SanitizeForMermaidId(graph.InitialNode);
            sb.AppendLine($"    Dev->>Engine: Start Instance");
            sb.AppendLine($"    Engine->>+{firstNodeName}: Execute Node");
            sb.AppendLine($"    {firstNodeName}-->>-Engine: Transition Ready");

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { graph.InitialNode };
            var queue = new Queue<string>();
            queue.Enqueue(graph.InitialNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (graph.Nodes.TryGetValue(current, out var node))
                {
                    foreach (var t in node.Transitions)
                    {
                        if (graph.Nodes.ContainsKey(t.TargetNode) && visited.Add(t.TargetNode))
                        {
                            var target = SanitizeForMermaidId(t.TargetNode);
                            var source = SanitizeForMermaidId(current);
                            
                            var msg = string.IsNullOrEmpty(t.Condition) ? "Transition" : $"Trigger: {t.Condition}";
                            sb.AppendLine($"    Engine->>+{target}: {msg}");
                            sb.AppendLine($"    {target}-->>-Engine: Complete");
                            
                            queue.Enqueue(t.TargetNode);
                        }
                    }
                }
            }
        }

        return sb.ToString();
    }

    private static string SanitizeForMermaidId(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "node";
        return new string(text.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string SanitizeForMermaidLabel(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Replace("\"", "'").Replace("\n", " ").Trim();
    }
}
