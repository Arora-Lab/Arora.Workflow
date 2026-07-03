using Arora.Workflow.Domain.Exceptions;
using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Aggregates;

/// <summary>
/// The aggregate root for a pending or completed approval decision.
/// </summary>
public sealed class Approval
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    /// <summary>The unique identifier of this approval.</summary>
    public Guid Id { get; private set; }

    /// <summary>The ID of the tenant this approval belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The ID of the workflow instance this approval belongs to.</summary>
    public Guid WorkflowInstanceId { get; private set; }

    // -------------------------------------------------------------------------
    // Details
    // -------------------------------------------------------------------------

    /// <summary>The name of the workflow definition (denormalized for display).</summary>
    public string WorkflowName { get; private set; } = default!;

    /// <summary>The business entity this workflow is about.</summary>
    public string CorrelationId { get; private set; } = default!;

    /// <summary>The name of the step that created this approval.</summary>
    public string StepName { get; private set; } = default!;

    /// <summary>The actor this approval is assigned to.</summary>
    public ActorInfo AssignedActor { get; private set; } = default!;

    // -------------------------------------------------------------------------
    // Status & Decision
    // -------------------------------------------------------------------------

    /// <summary>The current status of this approval.</summary>
    public ApprovalStatus Status { get; private set; }

    /// <summary>An optional comment provided when the decision was made.</summary>
    public string? Comment { get; private set; }

    // -------------------------------------------------------------------------
    // Timestamps
    // -------------------------------------------------------------------------

    /// <summary>The UTC time this approval was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The UTC deadline by which a decision must be made, if any.</summary>
    public DateTimeOffset? DeadlineAt { get; private set; }

    /// <summary>The UTC time this approval was decided.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>The actor who made the decision.</summary>
    public ActorInfo? DecidedByActor { get; private set; }

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    private Approval() { }

    /// <summary>
    /// Creates a new pending approval.
    /// </summary>
    public static Approval Create(
        Guid tenantId,
        Guid workflowInstanceId,
        string workflowName,
        string correlationId,
        string stepName,
        ActorInfo assignedActor,
        DateTimeOffset createdAt,
        DateTimeOffset? deadlineAt = null)
    {
        return new Approval
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkflowInstanceId = workflowInstanceId,
            WorkflowName = workflowName,
            CorrelationId = correlationId,
            StepName = stepName,
            AssignedActor = assignedActor,
            Status = ApprovalStatus.Pending,
            CreatedAt = createdAt,
            DeadlineAt = deadlineAt
        };
    }

    // -------------------------------------------------------------------------
    // Behaviours
    // -------------------------------------------------------------------------

    /// <summary>
    /// Records an approval decision.
    /// </summary>
    /// <exception cref="DuplicateApprovalException">Thrown if already decided.</exception>
    public void Approve(ActorInfo decidedBy, string? comment, DateTimeOffset clock)
    {
        if (Status != ApprovalStatus.Pending)
            throw new DuplicateApprovalException(Id, Status.ToString());

        Status = ApprovalStatus.Approved;
        Comment = comment;
        DecidedAt = clock;
        DecidedByActor = decidedBy;
    }

    /// <summary>
    /// Records a rejection decision.
    /// </summary>
    /// <exception cref="DuplicateApprovalException">Thrown if already decided.</exception>
    public void Reject(ActorInfo decidedBy, string? comment, DateTimeOffset clock)
    {
        if (Status != ApprovalStatus.Pending)
            throw new DuplicateApprovalException(Id, Status.ToString());

        Status = ApprovalStatus.Rejected;
        Comment = comment;
        DecidedAt = clock;
        DecidedByActor = decidedBy;
    }
}
