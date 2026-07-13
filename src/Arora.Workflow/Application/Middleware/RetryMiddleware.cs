using Arora.Workflow.Application.Steps;
using Microsoft.Extensions.Logging;

namespace Arora.Workflow.Application.Middleware;

/// <summary>
/// A workflow middleware that automatically retries step execution on unhandled exceptions.
/// </summary>
public sealed class RetryMiddleware : IWorkflowMiddleware
{
    private readonly ILogger<RetryMiddleware> _logger;

    public RetryMiddleware(ILogger<RetryMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<string?> InvokeAsync(
        StepExecutionContext context,
        WorkflowStepDelegate next)
    {
        // Default retry policy: 3 attempts with exponential backoff.
        // In a real enterprise system, this could be read from `context.StepDefinition.RetryPolicyJson`.
        const int maxAttempts = 3;
        int currentAttempt = 0;

        while (true)
        {
            currentAttempt++;

            try
            {
                var result = await next(context);
                
                // If it fails explicitly via StepResult.Failed, we don't automatically retry 
                // because it's a domain-handled failure. We only retry on unhandled exceptions.
                return result;
            }
            catch (Exception ex)
            {
                if (currentAttempt >= maxAttempts)
                {
                    _logger.LogError(
                        ex, 
                        "Step '{StepName}' failed after {MaxAttempts} attempts. Bubbling exception.", 
                        context.StepName, maxAttempts);
                    
                    throw;
                }

                var delayMs = (int)Math.Pow(2, currentAttempt) * 1000;
                
                _logger.LogWarning(
                    ex, 
                    "Step '{StepName}' failed on attempt {Attempt}. Retrying in {DelayMs}ms...", 
                    context.StepName, currentAttempt, delayMs);

                await Task.Delay(delayMs, context.CancellationToken);
            }
        }
    }
}
