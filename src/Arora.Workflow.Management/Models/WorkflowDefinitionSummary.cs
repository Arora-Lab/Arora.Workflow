using System;

namespace Arora.Workflow.Management.Models;

/// <summary>
/// A summary of a published workflow definition.
/// </summary>
public record WorkflowDefinitionSummary(
    Guid Id,
    string Name,
    int Version,
    int StepCount,
    DateTimeOffset CreatedAt);
