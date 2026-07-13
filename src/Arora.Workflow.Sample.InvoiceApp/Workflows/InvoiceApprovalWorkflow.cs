using Arora.Workflow.Application.Builder;
using Arora.Workflow.Application.Interfaces;

namespace Arora.Workflow.Sample.InvoiceApp.Workflows;

public class InvoiceApprovalWorkflow
{
    public string GetDefinitionJson()
    {
        var builder = new WorkflowDefinitionBuilder();
        
        builder
            .WithStep<Steps.ValidateInvoiceStep>("validate")
                .TransitionsTo("manager-approval")
            .WithApproval("manager-approval", "tester")
                .OnApprove("process-payment")
                .OnReject("send-rejection")
            .WithStep<Steps.ProcessPaymentStep>("process-payment")
            .WithStep<Steps.SendRejectionStep>("send-rejection");

        return builder.BuildJson();
    }
}
