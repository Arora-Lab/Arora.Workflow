using System;
using System.Collections.Generic;
using Arora.Workflow.Tooling.Layout;
using Arora.Workflow.Tooling.Diagnostics;

namespace Arora.Workflow.Management.Models;

/// <summary>
/// A detailed view of a published workflow definition, including its parsed layout and diagnostic warnings.
/// </summary>
public record WorkflowDefinitionDetails(
    Guid Id,
    string Name,
    int Version,
    string? Description,
    string DefinitionJson,
    DateTimeOffset CreatedAt,
    WorkflowLayout Layout,
    string Mermaid,
    List<WorkflowDiagnostic> Diagnostics);
