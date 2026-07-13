namespace Arora.Workflow.Application.Builder;

/// <summary>
/// A fluent builder for configuring a standard workflow step.
/// </summary>
public interface IStepBuilder
{
    /// <summary>
    /// Defines an unconditional transition to the next step.
    /// </summary>
    WorkflowDefinitionBuilder TransitionsTo(string nextStepName);
    
    /// <summary>
    /// Completes the configuration for this step node and returns to the workflow builder.
    /// </summary>
    WorkflowDefinitionBuilder EndStep();
}
