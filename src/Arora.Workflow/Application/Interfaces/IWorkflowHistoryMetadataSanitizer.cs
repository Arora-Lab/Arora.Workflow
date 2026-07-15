using System;
using System.Text.Json;
using Arora.Workflow.Domain.Events;

namespace Arora.Workflow.Application.Interfaces;

public record WorkflowHistoryMetadataContext(
    Guid TenantId,
    Guid WorkflowInstanceId,
    string EventType,
    string? StepName);

public interface IWorkflowHistoryMetadataSanitizer
{
    JsonElement? Sanitize(
        IWorkflowEvent domainEvent,
        WorkflowHistoryMetadataContext context);
}
