using System;

namespace Arora.Workflow.Management.Models;

/// <summary>
/// A history item for a workflow instance.
/// </summary>
public record WorkflowHistoryItem(
    Guid Id,
    Guid InstanceId,
    string? StepName,
    string Action,
    DateTimeOffset Timestamp,
    string? Actor,
    long Sequence,
    string? NodeId,
    string? FromState,
    string? ToState,
    string? Comment);
