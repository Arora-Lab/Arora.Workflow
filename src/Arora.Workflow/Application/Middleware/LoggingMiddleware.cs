using System.Diagnostics;
using Arora.Workflow.Application.Steps;
using Microsoft.Extensions.Logging;

namespace Arora.Workflow.Application.Middleware;

/// <summary>
/// A workflow middleware that logs the execution of each step, including its duration.
/// </summary>
public sealed class LoggingMiddleware : IWorkflowMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<string?> InvokeAsync(
        StepExecutionContext context,
        WorkflowStepDelegate next)
    {
        _logger.LogInformation(
            "Executing step '{StepName}' for workflow instance {InstanceId}",
            context.StepName, context.Instance.Id);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next(context);
            stopwatch.Stop();

            _logger.LogInformation(
                "Step '{StepName}' completed successfully in {ElapsedMilliseconds}ms",
                context.StepName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Step '{StepName}' threw an unhandled exception after {ElapsedMilliseconds}ms",
                context.StepName, stopwatch.ElapsedMilliseconds);

            throw; // Let the engine or RetryMiddleware handle it
        }
    }
}
