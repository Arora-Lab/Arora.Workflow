namespace Arora.Workflow.Management.Models;

/// <summary>
/// A filter for retrieving workflow instances.
/// </summary>
public record WorkflowInstanceFilter(
    string? DefinitionId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 25);
