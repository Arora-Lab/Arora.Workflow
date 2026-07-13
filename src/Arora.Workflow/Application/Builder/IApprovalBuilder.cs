using System;

namespace Arora.Workflow.Application.Builder;

/// <summary>
/// A fluent builder for configuring an approval node.
/// </summary>
public interface IApprovalBuilder
{
    /// <summary>
    /// Assigns the approval to a specific actor or role.
    /// </summary>
    IApprovalBuilder AssignedTo(string actorId);

    /// <summary>
    /// Defines the transition when the approval is approved.
    /// </summary>
    IApprovalBuilder OnApprove(string nextStepName);

    /// <summary>
    /// Defines the transition when the approval is rejected.
    /// </summary>
    IApprovalBuilder OnReject(string nextStepName);

    /// <summary>
    /// Completes the configuration for this approval node and returns to the workflow builder.
    /// </summary>
    WorkflowDefinitionBuilder EndApproval();
}
