using Arora.Workflow.Domain.Aggregates;

namespace Arora.Workflow.Application.Steps;

public class StepExecutionContext
{
    public required WorkflowInstance Instance { get; init; }
    public required string StepName { get; init; }
    public CancellationToken CancellationToken { get; init; }
}

public interface IWorkflowStep
{
    /// <summary>
    /// Executes the step logic.
    /// </summary>
    /// <returns>A string representing the condition for the next transition, or null for default transition.</returns>
    Task<string?> ExecuteAsync(StepExecutionContext context);
}
