using System;

namespace Arora.Workflow.Domain.Entities;

public enum WorkType
{
    ExecuteStep,
    RetryStep,
    ResumeWorkflow,
    ProcessDeadline,
    DispatchEvent
}

public enum WorkItemStatus
{
    Pending,
    Processing,
    Completed,
    DeadLettered
}

public class WorkItem
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid WorkflowInstanceId { get; private set; }
    public WorkType WorkType { get; private set; }
    public WorkItemStatus Status { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Payload { get; private set; }

    private WorkItem() { } // EF Core

    public WorkItem(
        Guid id,
        string tenantId,
        Guid workflowInstanceId,
        WorkType workType,
        DateTimeOffset availableAt,
        string? payload = null)
    {
        Id = id;
        TenantId = tenantId;
        WorkflowInstanceId = workflowInstanceId;
        WorkType = workType;
        Status = WorkItemStatus.Pending;
        AvailableAt = availableAt;
        AttemptCount = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        Payload = payload;
    }

    public void Claim(string workerId, TimeSpan leaseDuration)
    {
        if (Status != WorkItemStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot claim WorkItem in status {Status}.");
        }

        Status = WorkItemStatus.Processing;
        LockedBy = workerId;
        LockedUntil = DateTimeOffset.UtcNow.Add(leaseDuration);
        AttemptCount++;
    }

    public void Complete()
    {
        Status = WorkItemStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        LockedBy = null;
        LockedUntil = null;
    }

    public void FailTransiently(string error, DateTimeOffset nextAvailableAt)
    {
        Status = WorkItemStatus.Pending;
        LastError = error;
        AvailableAt = nextAvailableAt;
        LockedBy = null;
        LockedUntil = null;
    }

    public void FailPermanently(string error)
    {
        Status = WorkItemStatus.DeadLettered;
        LastError = error;
        LockedBy = null;
        LockedUntil = null;
    }
}
