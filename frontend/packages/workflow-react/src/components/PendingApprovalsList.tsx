import React, { useState } from 'react';
import { usePendingApprovals } from '../hooks/usePendingApprovals';

export interface PendingApprovalsListProps {
  onSelectInstance?: (instanceId: string) => void;
}

export const PendingApprovalsList: React.FC<PendingApprovalsListProps> = ({ onSelectInstance }) => {
  const { approvals, loading, error, approve, reject } = usePendingApprovals();
  const [submittingId, setSubmittingId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const handleAction = async (id: string, type: 'approve' | 'reject') => {
    setSubmittingId(id);
    setActionError(null);
    try {
      if (type === 'approve') {
        await approve(id);
      } else {
        await reject(id);
      }
    } catch (err: any) {
      setActionError(err?.message || `Failed to submit ${type} decision.`);
    } finally {
      setSubmittingId(null);
    }
  };

  if (loading && approvals.length === 0) {
    return <div className="arora-loading-spinner" />;
  }

  if (error) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state" style={{ borderColor: 'var(--arora-color-rejected)' }}>
          <p style={{ color: 'var(--arora-color-rejected)', fontWeight: 'bold' }}>Error loading approvals</p>
          <p style={{ fontSize: '13px' }}>{error.message}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="arora-card">
      <h3 className="arora-card-title">
        <span>Pending Approvals</span>
        <span style={{ fontSize: '12px', fontWeight: 'normal', color: 'var(--arora-text-muted)' }}>
          Assigned to you: {approvals.length}
        </span>
      </h3>

      {actionError && (
        <div
          style={{
            background: 'rgba(239, 68, 68, 0.1)',
            border: '1px solid var(--arora-color-rejected)',
            color: 'var(--arora-color-rejected)',
            padding: '12px',
            borderRadius: 'var(--arora-radius-sm)',
            fontSize: '13px',
          }}
        >
          {actionError}
        </div>
      )}

      {approvals.length === 0 ? (
        <div className="arora-empty-state">
          <svg
            width="24"
            height="24"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <circle cx="12" cy="12" r="10" />
            <path d="m9 12 2 2 4-4" />
          </svg>
          <p>You have no pending approvals. Nice job!</p>
        </div>
      ) : (
        <div className="arora-approval-grid">
          {approvals.map((app) => {
            const isSubmitting = submittingId === app.approvalId;
            return (
              <div key={app.approvalId} className="arora-approval-card">
                <div className="arora-approval-card-header">
                  <div>
                    <h4 className="arora-approval-title">{app.workflowName}</h4>
                    <span className="arora-approval-subtitle">Step: {app.stepName}</span>
                  </div>
                  <button
                    className="arora-btn arora-btn-secondary"
                    onClick={() => onSelectInstance?.(app.workflowInstanceId)}
                    style={{ padding: '4px 8px', fontSize: '11px' }}
                  >
                    View Instance
                  </button>
                </div>

                <div className="arora-approval-metadata">
                  <span className="arora-approval-meta-label">Correlation ID</span>
                  <span className="arora-approval-meta-value" style={{ fontFamily: 'monospace' }}>
                    {app.correlationId}
                  </span>

                  <span className="arora-approval-meta-label">Assigned</span>
                  <span className="arora-approval-meta-value">
                    {new Date(app.createdAt).toLocaleDateString()}
                  </span>

                  {app.deadlineAt && (
                    <>
                      <span className="arora-approval-meta-label" style={{ color: 'var(--arora-color-rejected)' }}>
                        Deadline
                      </span>
                      <span className="arora-approval-meta-value" style={{ color: 'var(--arora-color-rejected)' }}>
                        {new Date(app.deadlineAt).toLocaleString()}
                      </span>
                    </>
                  )}
                </div>

                <div className="arora-approval-actions">
                  <button
                    className="arora-btn arora-btn-primary"
                    disabled={isSubmitting}
                    onClick={() => handleAction(app.approvalId, 'approve')}
                  >
                    {isSubmitting ? 'Approving...' : 'Approve'}
                  </button>
                  <button
                    className="arora-btn arora-btn-danger"
                    disabled={isSubmitting}
                    onClick={() => handleAction(app.approvalId, 'reject')}
                  >
                    Reject
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
