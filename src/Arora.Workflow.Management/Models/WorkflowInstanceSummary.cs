using System;

namespace Arora.Workflow.Management.Models;

/// <summary>
/// A summary of a running or completed workflow instance.
/// </summary>
public record WorkflowInstanceSummary(
    Guid Id,
    Guid WorkflowDefinitionId,
    int WorkflowDefinitionVersion,
    string Status,
    string CurrentState,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);
