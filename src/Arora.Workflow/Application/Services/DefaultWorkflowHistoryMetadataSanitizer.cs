using System;
using System.Collections.Generic;
using System.Text.Json;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Events;

namespace Arora.Workflow.Application.Services;

public sealed class DefaultWorkflowHistoryMetadataSanitizer : IWorkflowHistoryMetadataSanitizer
{
    private const int MaxMetadataStringLength = 2000;
    private const int MaxNestingDepth = 3;

    public JsonElement? Sanitize(IWorkflowEvent domainEvent, WorkflowHistoryMetadataContext context)
    {
        if (domainEvent == null) return null;

        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Allowlist mapping based on event type
        switch (domainEvent)
        {
            case WorkflowStarted e:
                dictionary["WorkflowName"] = e.WorkflowName;
                dictionary["WorkflowVersion"] = e.WorkflowVersion;
                dictionary["CorrelationId"] = e.CorrelationId;
                break;

            case WorkflowTransitioned e:
                dictionary["FromState"] = e.FromState;
                dictionary["ToState"] = e.ToState;
                dictionary["StepName"] = e.StepName;
                break;

            case WorkflowCancelled e:
                dictionary["Reason"] = e.Reason;
                dictionary["LastActiveState"] = e.LastActiveState;
                break;

            case WorkflowCompleted e:
                dictionary["WorkflowName"] = e.WorkflowName;
                dictionary["CorrelationId"] = e.CorrelationId;
                dictionary["TotalDurationMs"] = e.TotalDurationMs;
                break;

            case WorkflowRejected e:
                dictionary["WorkflowName"] = e.WorkflowName;
                dictionary["CorrelationId"] = e.CorrelationId;
                dictionary["RejectedAtStep"] = e.RejectedAtStep;
                break;
        }

        try
        {
            // Serialize to enforce string constraints and depth checks
            var options = new JsonSerializerOptions
            {
                MaxDepth = MaxNestingDepth,
                WriteIndented = false
            };

            var serialized = JsonSerializer.Serialize(dictionary, options);
            if (serialized.Length > MaxMetadataStringLength)
            {
                // Fallback to minimal set if size is exceeded
                var fallback = new Dictionary<string, object?>
                {
                    ["Error"] = "Metadata size limit exceeded"
                };
                serialized = JsonSerializer.Serialize(fallback, options);
            }

            using var doc = JsonDocument.Parse(serialized);
            return doc.RootElement.Clone(); // Clone to prevent resource disposal issues
        }
        catch
        {
            return null;
        }
    }
}
