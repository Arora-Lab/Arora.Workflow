using Arora.Workflow.Internal.Engine.Graph;

namespace Arora.Workflow.Tooling.Layout;

/// <summary>
/// Defines a contract for calculating node coordinates and transition link lines for a workflow definition.
/// </summary>
public interface IWorkflowLayoutEngine
{
    /// <summary>
    /// Computes the visual layout (coordinates and connections) for a given workflow graph.
    /// </summary>
    /// <param name="graph">The parsed workflow graph definition.</param>
    /// <returns>A layout describing where to place nodes and draw connections.</returns>
    WorkflowLayout ComputeLayout(WorkflowGraph graph);
}
