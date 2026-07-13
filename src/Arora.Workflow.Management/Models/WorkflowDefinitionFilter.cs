namespace Arora.Workflow.Management.Models;

/// <summary>
/// A filter for retrieving workflow definitions.
/// </summary>
public record WorkflowDefinitionFilter(
    int Page = 1,
    int PageSize = 25);
