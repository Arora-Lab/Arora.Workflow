using System;
using System.Threading.Tasks;
using Arora.Workflow.Application.Steps;

namespace Arora.Workflow.Application.Middleware;

/// <summary>
/// A delegate representing the next middleware or step in the pipeline.
/// </summary>
public delegate Task<string?> WorkflowStepDelegate(StepExecutionContext context);

public interface IWorkflowMiddleware
{
    /// <summary>
    /// Executes the middleware logic.
    /// </summary>
    Task<string?> InvokeAsync(StepExecutionContext context, WorkflowStepDelegate next);
}
