namespace Arora.Workflow.Domain.ValueObjects;

/// <summary>
/// The lifecycle status of a WorkflowDefinition.
/// </summary>
public enum WorkflowDefinitionStatus
{
    /// <summary>
    /// The definition is being authored and is not yet available for new instances.
    /// A Draft definition may be edited freely.
    /// </summary>
    Draft,

    /// <summary>
    /// The definition is active and available for new workflow instances.
    /// A Published definition is immutable — any changes require a new version.
    /// </summary>
    Published,

    /// <summary>
    /// The definition has been superseded and will not accept new instances.
    /// Existing instances running against this version continue to completion.
    /// </summary>
    Deprecated
}
