using Arora.Workflow.Application.Steps;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Arora.Workflow.Sample.InvoiceApp.Workflows.Steps;

public class ValidateInvoiceStep : IWorkflowStep
{
    public Task<string?> ExecuteAsync(StepExecutionContext context)
    {
        // For our sample, just return null to transition to the next default step
        return Task.FromResult<string?>(null);
    }
}
