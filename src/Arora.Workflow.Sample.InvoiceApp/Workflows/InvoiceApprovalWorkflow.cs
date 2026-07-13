using Arora.Workflow.Application.Builder;

namespace Arora.Workflow.Sample.InvoiceApp.Workflows;

public class InvoiceApprovalWorkflow
{
    public (string Name, int Version, string Description, string Json) GetDefinition()
    {
        return WorkflowDefinitionBuilder.Create("invoice-approval")
            .Description("Basic Invoice Approval Process")
            .Version(1)
            .WithStep<Steps.ValidateInvoiceStep>("validate")
                .TransitionsTo("manager-approval")
            .WithApproval("manager-approval")
                .AssignedTo("tester")
                .OnApprove("process-payment")
                .OnReject("send-rejection")
                .EndApproval()
            .WithStep<Steps.ProcessPaymentStep>("process-payment")
                .EndStep()
            .WithStep<Steps.SendRejectionStep>("send-rejection")
                .EndStep()
            .Build();
    }
}
