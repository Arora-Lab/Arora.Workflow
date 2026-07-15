import React from 'react';
import { useWorkflowInstanceHistory } from '../hooks/useWorkflowInstanceHistory';

export interface HistoryTimelineProps {
  instanceId: string;
}

export const HistoryTimeline: React.FC<HistoryTimelineProps> = ({ instanceId }) => {
  const { history, loading, error } = useWorkflowInstanceHistory(instanceId);

  if (!instanceId) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state">
          <p>Select a workflow instance to view its execution history.</p>
        </div>
      </div>
    );
  }

  if (loading && history.length === 0) {
    return <div className="arora-loading-spinner" />;
  }

  if (error) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state" style={{ borderColor: 'var(--arora-color-rejected)' }}>
          <p style={{ color: 'var(--arora-color-rejected)', fontWeight: 'bold' }}>Error loading history</p>
          <p style={{ fontSize: '13px' }}>{error.message}</p>
        </div>
      </div>
    );
  }

  if (history.length === 0) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state">
          <p>No history records found for this instance.</p>
        </div>
      </div>
    );
  }

  const getNodeColor = (action: string) => {
    switch (action.toLowerCase()) {
      case 'started':
        return 'var(--arora-color-running)';
      case 'completed':
      case 'stepexecuted':
      case 'approvalgranted':
        return 'var(--arora-color-completed)';
      case 'rejected':
      case 'approvalrejected':
      case 'stepfailed':
        return 'var(--arora-color-rejected)';
      case 'cancelled':
        return 'var(--arora-color-cancelled)';
      case 'approvalrequested':
        return 'var(--arora-color-pending)';
      default:
        return 'var(--arora-text-dark)';
    }
  };

  return (
    <div className="arora-card">
      <h3 className="arora-card-title">Execution History</h3>

      <div className="arora-timeline">
        {history.map((item) => (
          <div key={item.id} className="arora-timeline-item">
            <div
              className="arora-timeline-node"
              style={{
                backgroundColor: getNodeColor(item.action),
                boxShadow: `0 0 8px ${getNodeColor(item.action)}`,
              }}
            />
            <div className="arora-timeline-content">
              <div className="arora-timeline-header">
                <span style={{ fontWeight: '700', fontSize: '14px' }}>
                  {item.action}
                </span>
                {item.stepName && (
                  <span style={{ color: 'var(--arora-text-muted)', fontSize: '13px' }}>
                    at <strong>{item.stepName}</strong>
                  </span>
                )}
              </div>
              <div className="arora-timeline-time">
                {new Date(item.timestamp).toLocaleString()}
                {item.actor && ` by ${item.actor}`}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
