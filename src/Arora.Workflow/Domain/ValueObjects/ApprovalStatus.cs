namespace Arora.Workflow.Domain.ValueObjects;

/// <summary>
/// The status of an approval request.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// The approval is pending a decision.
    /// </summary>
    Pending,

    /// <summary>
    /// The approval has been granted.
    /// </summary>
    Approved,

    /// <summary>
    /// The approval has been rejected.
    /// </summary>
    Rejected
}
