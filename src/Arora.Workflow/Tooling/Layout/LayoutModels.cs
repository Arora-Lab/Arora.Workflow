using System.Collections.Generic;

namespace Arora.Workflow.Tooling.Layout;

/// <summary>
/// Represents a point in 2D space.
/// </summary>
public record LayoutPoint(double X, double Y);

/// <summary>
/// Represents the layout coordinates and dimensions for a single node.
/// </summary>
public record NodeLayout(
    string Name,
    string Type,
    double X,
    double Y,
    double Width,
    double Height);

/// <summary>
/// Represents the layout line path for a transition between two nodes.
/// </summary>
public record ConnectionLayout(
    string FromNode,
    string ToNode,
    string? Condition,
    List<LayoutPoint> Points);

/// <summary>
/// The complete computed visual layout for a workflow graph.
/// </summary>
public record WorkflowLayout(
    List<NodeLayout> Nodes,
    List<ConnectionLayout> Connections);
