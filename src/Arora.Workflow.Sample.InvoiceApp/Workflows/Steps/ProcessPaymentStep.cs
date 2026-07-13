using Arora.Workflow.Application.Steps;
using System.Text.Json.Nodes;

namespace Arora.Workflow.Sample.InvoiceApp.Workflows.Steps;

public class ProcessPaymentStep : IWorkflowStep
{
    public Task<string?> ExecuteAsync(StepExecutionContext context)
    {
        return Task.FromResult<string?>(null);
    }
}
