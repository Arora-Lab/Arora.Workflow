using System;
using System.Collections.Generic;
using System.Linq;
using Arora.Workflow.Internal.Engine.Graph;

namespace Arora.Workflow.Tooling.Diagnostics;

/// <summary>
/// Specifies the severity of a workflow diagnostic report.
/// </summary>
public enum DiagnosticSeverity
{
    Error,
    Warning,
    Suggestion,
    Info
}

/// <summary>
/// Represents a validation diagnostic message.
/// </summary>
public record WorkflowDiagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    string? NodeName,
    string? Suggestion);

/// <summary>
/// Roslyn-style diagnostic engine for running structural validation checks on workflow definitions.
/// </summary>
public class WorkflowDiagnosticsEngine
{
    private static readonly string[] ReservedNames = { "start", "end", "cancel", "fail", "terminate", "error" };

    /// <summary>
    /// Analyzes a workflow graph and reports errors, warnings, and suggestions.
    /// </summary>
    public static List<WorkflowDiagnostic> Analyze(WorkflowGraph graph)
    {
        var diagnostics = new List<WorkflowDiagnostic>();

        if (graph == null)
        {
            diagnostics.Add(new WorkflowDiagnostic(
                Code: "AW_000_NULL_GRAPH",
                Severity: DiagnosticSeverity.Error,
                Message: "Workflow graph definition is null or unparseable.",
                NodeName: null,
                Suggestion: "Check if the workflow definition JSON is valid."
            ));
            return diagnostics;
        }

        if (string.IsNullOrWhiteSpace(graph.InitialNode))
        {
            diagnostics.Add(new WorkflowDiagnostic(
                Code: "AW_006_MISSING_START",
                Severity: DiagnosticSeverity.Error,
                Message: "Workflow has no initial node (Start Node) defined.",
                NodeName: null,
                Suggestion: "Call .WithStep<T> or .WithApproval first to set the initial entrypoint node."
            ));
            return diagnostics;
        }

        if (!graph.Nodes.ContainsKey(graph.InitialNode))
        {
            diagnostics.Add(new WorkflowDiagnostic(
                Code: "AW_006_INVALID_START",
                Severity: DiagnosticSeverity.Error,
                Message: $"Initial node '{graph.InitialNode}' is declared but does not exist in the nodes collection.",
                NodeName: graph.InitialNode,
                Suggestion: "Make sure you create a step or approval with name corresponding to the initial node."
            ));
        }

        // Run Rule Checks
        CheckReservedAndDuplicateNames(graph, diagnostics);
        CheckUnreachableAndOrphanNodes(graph, diagnostics);
        CheckCycles(graph, diagnostics);
        CheckDeadEndsAndTransitions(graph, diagnostics);

        return diagnostics;
    }

    private static void CheckReservedAndDuplicateNames(WorkflowGraph graph, List<WorkflowDiagnostic> diagnostics)
    {
        foreach (var nodeName in graph.Nodes.Keys)
        {
            if (ReservedNames.Contains(nodeName.ToLowerInvariant()))
            {
                diagnostics.Add(new WorkflowDiagnostic(
                    Code: "AW_005_RESERVED_NAMES",
                    Severity: DiagnosticSeverity.Warning,
                    Message: $"Node name '{nodeName}' uses a reserved system word.",
                    NodeName: nodeName,
                    Suggestion: $"Consider renaming to something more specific (e.g., 'Submit{nodeName}' or '{nodeName}Step')."
                ));
            }
        }
    }

    private static void CheckUnreachableAndOrphanNodes(WorkflowGraph graph, List<WorkflowDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(graph.InitialNode) || !graph.Nodes.ContainsKey(graph.InitialNode))
        {
            return;
        }

        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();

        queue.Enqueue(graph.InitialNode);
        reachable.Add(graph.InitialNode);

        while (queue.Count > 0)
            {
            var current = queue.Dequeue();
            if (graph.Nodes.TryGetValue(current, out var node))
            {
                foreach (var transition in node.Transitions)
                {
                    if (graph.Nodes.ContainsKey(transition.TargetNode))
                    {
                        if (reachable.Add(transition.TargetNode))
                        {
                            queue.Enqueue(transition.TargetNode);
                        }
                    }
                }
            }
        }

        foreach (var nodeName in graph.Nodes.Keys)
        {
            if (!reachable.Contains(nodeName))
            {
                diagnostics.Add(new WorkflowDiagnostic(
                    Code: "AW_002_ORPHANS",
                    Severity: DiagnosticSeverity.Warning,
                    Message: $"Node '{nodeName}' is unreachable from the start node '{graph.InitialNode}'.",
                    NodeName: nodeName,
                    Suggestion: "Connect this node to the workflow flow using a transition from an active node."
                ));
            }
        }
    }

    private static void CheckCycles(WorkflowGraph graph, List<WorkflowDiagnostic> diagnostics)
    {
        var visited = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0=unvisited, 1=visiting, 2=visited
        foreach (var name in graph.Nodes.Keys)
        {
            visited[name] = 0;
        }

        foreach (var nodeName in graph.Nodes.Keys)
        {
            if (visited[nodeName] == 0)
            {
                if (HasCycleDfs(graph, nodeName, visited, out var offendingNode))
                {
                    diagnostics.Add(new WorkflowDiagnostic(
                        Code: "AW_001_CYCLES",
                        Severity: DiagnosticSeverity.Warning,
                        Message: "A cyclical path (infinite loop) was detected in the workflow.",
                        NodeName: offendingNode,
                        Suggestion: "Ensure loops contain a path out, such as a conditional transition, to prevent infinite loops."
                    ));
                    break; // report cycle warning once to avoid spam
                }
            }
        }
    }

    private static bool HasCycleDfs(WorkflowGraph graph, string current, Dictionary<string, int> visited, out string? offendingNode)
    {
        visited[current] = 1; // visiting

        if (graph.Nodes.TryGetValue(current, out var node))
        {
            foreach (var t in node.Transitions)
            {
                if (graph.Nodes.ContainsKey(t.TargetNode))
                {
                    if (visited[t.TargetNode] == 1)
                    {
                        offendingNode = current;
                        return true;
                    }

                    if (visited[t.TargetNode] == 0)
                    {
                        if (HasCycleDfs(graph, t.TargetNode, visited, out offendingNode))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        visited[current] = 2; // visited
        offendingNode = null;
        return false;
    }

    private static void CheckDeadEndsAndTransitions(WorkflowGraph graph, List<WorkflowDiagnostic> diagnostics)
    {
        foreach (var nodeKv in graph.Nodes)
        {
            var node = nodeKv.Value;
            var name = nodeKv.Key;

            if (node.Transitions.Count == 0)
            {
                diagnostics.Add(new WorkflowDiagnostic(
                    Code: "AW_003_DEAD_ENDS",
                    Severity: DiagnosticSeverity.Warning,
                    Message: $"Node '{name}' has no outgoing transitions. It acts as a terminal state.",
                    NodeName: name,
                    Suggestion: "If this is not intended to be a final state, add a transition to lead to subsequent steps."
                ));
            }
            else
            {
                // Check AW_004: Duplicate triggers or identical target transitions
                var triggerCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var targetCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var t in node.Transitions)
                {
                    var cond = t.Condition ?? string.Empty;
                    triggerCounts[cond] = triggerCounts.GetValueOrDefault(cond) + 1;
                    targetCounts[t.TargetNode] = targetCounts.GetValueOrDefault(t.TargetNode) + 1;
                }

                foreach (var kv in triggerCounts)
                {
                    if (kv.Value > 1)
                    {
                        string triggerName = string.IsNullOrEmpty(kv.Key) ? "unconditional" : $"'{kv.Key}'";
                        diagnostics.Add(new WorkflowDiagnostic(
                            Code: "AW_004_DUPLICATE_TRANSITIONS",
                            Severity: DiagnosticSeverity.Error,
                            Message: $"Node '{name}' has duplicate transition triggers for {triggerName}.",
                            NodeName: name,
                            Suggestion: "Make sure transition triggers (conditions like 'Approved' or 'Rejected') are unique for this node."
                        ));
                    }
                }

                foreach (var kv in targetCounts)
                {
                    if (kv.Value > 1)
                    {
                        diagnostics.Add(new WorkflowDiagnostic(
                            Code: "AW_004_DUPLICATE_TRANSITIONS",
                            Severity: DiagnosticSeverity.Suggestion,
                            Message: $"Node '{name}' transitions to target '{kv.Key}' multiple times.",
                            NodeName: name,
                            Suggestion: "Simplify transitions by keeping a single unique route to each target node."
                        ));
                    }
                }
            }
        }
    }
}
