using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;

namespace Arora.Workflow.Application.Services;

/// <summary>
/// Maps domain aggregates to read-model DTOs.
/// Static methods with no dependencies — purely a data projection.
/// </summary>
internal static class WorkflowMapper
{
    /// <summary>
    /// Projects a <see cref="WorkflowInstance"/> aggregate to a
    /// <see cref="WorkflowInstanceSnapshot"/> read model.
    /// </summary>
    internal static WorkflowInstanceSnapshot ToSnapshot(WorkflowInstance instance) =>
        new()
        {
            Id               = instance.Id,
            WorkflowName     = instance.WorkflowName,
            WorkflowVersion  = instance.WorkflowDefinitionVersion,
            CorrelationId    = instance.CorrelationId,
            CurrentState     = instance.CurrentState,
            Status           = instance.Status,
            CreatedBy        = instance.CreatedBy,
            CreatedAt        = instance.CreatedAt,
            ModifiedAt       = instance.ModifiedAt,
            CompletedAt      = instance.CompletedAt
        };
}
