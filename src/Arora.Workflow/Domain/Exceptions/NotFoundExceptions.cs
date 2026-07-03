namespace Arora.Workflow.Domain.Exceptions;

/// <summary>
/// Thrown when a WorkflowInstance with the specified ID does not exist
/// in the current tenant's scope.
/// </summary>
public sealed class WorkflowNotFoundException : WorkflowException
{
    /// <summary>The ID that was not found.</summary>
    public Guid InstanceId { get; }

    /// <param name="instanceId">The ID that was not found.</param>
    public WorkflowNotFoundException(Guid instanceId)
        : base($"Workflow instance '{instanceId}' was not found.", "WORKFLOW_NOT_FOUND")
    {
        InstanceId = instanceId;
    }
}

/// <summary>
/// Thrown when a WorkflowDefinition with the specified name and version
/// does not exist or has not been published.
/// </summary>
public sealed class WorkflowDefinitionNotFoundException : WorkflowException
{
    /// <summary>The definition name that was not found.</summary>
    public string WorkflowName { get; }

    /// <summary>The version that was requested. Null means the latest version was requested.</summary>
    public int? Version { get; }

    /// <param name="workflowName">The definition name that was not found.</param>
    /// <param name="version">The specific version requested, or null if latest was requested.</param>
    public WorkflowDefinitionNotFoundException(string workflowName, int? version = null)
        : base(
            version.HasValue
                ? $"Workflow definition '{workflowName}' version {version} was not found or is not published."
                : $"No published workflow definition named '{workflowName}' was found.",
            "DEFINITION_NOT_FOUND")
    {
        WorkflowName = workflowName;
        Version = version;
    }
}
