using System;

namespace Arora.Workflow.Management.Models;

/// <summary>
/// Detailed view of a workflow instance.
/// </summary>
public record WorkflowInstanceDetails(
    Guid Id,
    Guid WorkflowDefinitionId,
    int WorkflowDefinitionVersion,
    string Status,
    string CurrentState,
    string? InputJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);
