using System;
using System.Collections.Generic;
using System.Linq;
using Arora.Workflow.Internal.Engine.Graph;

namespace Arora.Workflow.Tooling.Layout;

/// <summary>
/// A default implementation of <see cref="IWorkflowLayoutEngine"/> that arranges nodes in layers
/// based on their depth in the transition graph (Breadth-First Search-based layering).
/// </summary>
public class LayeredLayoutEngine : IWorkflowLayoutEngine
{
    private const double NodeWidth = 180;
    private const double NodeHeight = 60;
    private const double HorizontalSpacing = 100;
    private const double VerticalSpacing = 100;
    private const double MarginX = 50;
    private const double MarginY = 50;

    /// <inheritdoc />
    public WorkflowLayout ComputeLayout(WorkflowGraph graph)
    {
        var nodeLayouts = new List<NodeLayout>();
        var connectionLayouts = new List<ConnectionLayout>();

        if (graph?.Nodes == null || graph.Nodes.Count == 0)
        {
            return new WorkflowLayout(nodeLayouts, connectionLayouts);
        }

        // 1. Layer assignment using BFS to find depth levels
        var nodeLayers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        if (!string.IsNullOrEmpty(graph.InitialNode) && graph.Nodes.ContainsKey(graph.InitialNode))
        {
            var queue = new Queue<(string Name, int Level)>();
            queue.Enqueue((graph.InitialNode, 0));
            nodeLayers[graph.InitialNode] = 0;

            while (queue.Count > 0)
            {
                var (currentName, level) = queue.Dequeue();

                if (graph.Nodes.TryGetValue(currentName, out var currentNode))
                {
                    foreach (var transition in currentNode.Transitions)
                    {
                        if (graph.Nodes.ContainsKey(transition.TargetNode))
                        {
                            var targetName = transition.TargetNode;
                            var nextLevel = level + 1;

                            // If we find a deeper path to the node, update its level
                            if (!nodeLayers.TryGetValue(targetName, out var existingLevel) || nextLevel > existingLevel)
                            {
                                // Prevent infinite loops on cycles by limiting maximum level depth to node count
                                if (nextLevel < graph.Nodes.Count)
                                {
                                    nodeLayers[targetName] = nextLevel;
                                    queue.Enqueue((targetName, nextLevel));
                                }
                            }
                        }
                    }
                }
            }
        }

        // Add any orphan/unreachable nodes to a final layer
        int maxReachableLevel = nodeLayers.Count > 0 ? nodeLayers.Values.Max() : 0;
        foreach (var nodeName in graph.Nodes.Keys)
        {
            if (!nodeLayers.ContainsKey(nodeName))
            {
                nodeLayers[nodeName] = maxReachableLevel + 1;
            }
        }

        // Group nodes by their layer level
        var layerGroups = nodeLayers
            .GroupBy(kv => kv.Value)
            .OrderBy(g => g.Key)
            .ToList();

        // 2. Assign coordinates (X, Y) to each node
        var nodeCoords = new Dictionary<string, (double X, double Y)>(StringComparer.OrdinalIgnoreCase);

        for (int layerIndex = 0; layerIndex < layerGroups.Count; layerIndex++)
        {
            var group = layerGroups[layerIndex];
            var nodesInLayer = group.Select(kv => kv.Key).ToList();
            
            double y = MarginY + (layerIndex * (NodeHeight + VerticalSpacing));

            for (int nodeIndex = 0; nodeIndex < nodesInLayer.Count; nodeIndex++)
            {
                var nodeName = nodesInLayer[nodeIndex];
                double x = MarginX + (nodeIndex * (NodeWidth + HorizontalSpacing));

                nodeCoords[nodeName] = (x, y);

                if (graph.Nodes.TryGetValue(nodeName, out var node))
                {
                    nodeLayouts.Add(new NodeLayout(
                        Name: node.Name,
                        Type: node.Type,
                        X: x,
                        Y: y,
                        Width: NodeWidth,
                        Height: NodeHeight
                    ));
                }
            }
        }

        // 3. Generate connections (link lines)
        foreach (var nodeName in graph.Nodes.Keys)
        {
            if (graph.Nodes.TryGetValue(nodeName, out var node) && nodeCoords.TryGetValue(nodeName, out var sourceCoords))
            {
                foreach (var transition in node.Transitions)
                {
                    if (nodeCoords.TryGetValue(transition.TargetNode, out var targetCoords))
                    {
                        var points = new List<LayoutPoint>();

                        double startX = sourceCoords.X + (NodeWidth / 2);
                        double startY = sourceCoords.Y + NodeHeight;
                        double endX = targetCoords.X + (NodeWidth / 2);
                        double endY = targetCoords.Y;

                        // Same vertical layer (horizontal layout link)
                        if (Math.Abs(startY - endY) < 1)
                        {
                            startX = sourceCoords.X + NodeWidth;
                            startY = sourceCoords.Y + (NodeHeight / 2);
                            endX = targetCoords.X;
                            endY = targetCoords.Y + (NodeHeight / 2);

                            points.Add(new LayoutPoint(startX, startY));
                            points.Add(new LayoutPoint(startX + (HorizontalSpacing / 2), startY));
                            points.Add(new LayoutPoint(endX - (HorizontalSpacing / 2), endY));
                            points.Add(new LayoutPoint(endX, endY));
                        }
                        else
                        {
                            // Vertical layout link with orthopedic/S-curve intermediate control points
                            points.Add(new LayoutPoint(startX, startY));
                            points.Add(new LayoutPoint(startX, startY + (VerticalSpacing / 3)));
                            points.Add(new LayoutPoint(endX, endY - (VerticalSpacing / 3)));
                            points.Add(new LayoutPoint(endX, endY));
                        }

                        connectionLayouts.Add(new ConnectionLayout(
                            FromNode: node.Name,
                            ToNode: transition.TargetNode,
                            Condition: transition.Condition,
                            Points: points
                        ));
                    }
                }
            }
        }

        return new WorkflowLayout(nodeLayouts, connectionLayouts);
    }
}
