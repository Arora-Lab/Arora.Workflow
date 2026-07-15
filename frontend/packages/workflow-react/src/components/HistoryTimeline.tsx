import React from 'react';
import { useWorkflowInstanceHistory } from '../hooks/useWorkflowInstanceHistory';
import type { WorkflowHistoryItem } from '@arora/workflow-client';

export interface HistoryTimelineProps {
  instanceId: string;
  historyItems?: WorkflowHistoryItem[];
  selectedSequence?: number | null;
  onSelectSequence?: (seq: number) => void;
}

export const HistoryTimeline: React.FC<HistoryTimelineProps> = ({
  instanceId,
  historyItems,
  selectedSequence,
  onSelectSequence,
}) => {
  const { history: fetchedHistory, loading, error } = useWorkflowInstanceHistory(
    historyItems ? '' : instanceId
  );

  const history = historyItems || fetchedHistory;

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

  const handleKeyDown = (e: React.KeyboardEvent, index: number) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      const nextBtn = document.getElementById(`timeline-btn-${index + 1}`);
      if (nextBtn) {
        nextBtn.focus();
        (nextBtn as HTMLButtonElement).click();
      }
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      const prevBtn = document.getElementById(`timeline-btn-${index - 1}`);
      if (prevBtn) {
        prevBtn.focus();
        (prevBtn as HTMLButtonElement).click();
      }
    }
  };

  return (
    <div className="arora-card">
      <h3 className="arora-card-title">Execution History</h3>

      <div className="arora-timeline" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
        {history.map((item, idx) => {
          const isSelected = selectedSequence === item.sequence;
          return (
            <button
              id={`timeline-btn-${idx}`}
              key={item.id}
              className={`arora-timeline-item-btn ${isSelected ? 'active' : ''}`}
              onClick={() => onSelectSequence?.(item.sequence ?? 0)}
              onKeyDown={(e) => handleKeyDown(e, idx)}
              style={{
                display: 'flex',
                alignItems: 'center',
                width: '100%',
                background: 'none',
                border: 'none',
                padding: '8px 12px',
                textAlign: 'left',
                cursor: 'pointer',
                color: 'inherit',
                borderRadius: '6px',
                outline: 'none',
                transition: 'background 0.2s',
                backgroundColor: isSelected ? 'rgba(59, 130, 246, 0.15)' : 'transparent',
                borderLeft: isSelected ? '3px solid #3b82f6' : '3px solid transparent'
              }}
            >
              <div
                className="arora-timeline-node"
                style={{
                  backgroundColor: getNodeColor(item.action || ''),
                  boxShadow: `0 0 8px ${getNodeColor(item.action || '')}`,
                  marginRight: '12px',
                  width: '12px',
                  height: '12px',
                  borderRadius: '50%',
                  flexShrink: 0
                }}
              />
              <div className="arora-timeline-content" style={{ flexGrow: 1 }}>
                <div className="arora-timeline-header" style={{ display: 'flex', gap: '8px', alignItems: 'baseline' }}>
                  <span style={{ fontWeight: '700', fontSize: '14px' }}>
                    {item.action || 'Unknown Action'}
                  </span>
                  {item.stepName && (
                    <span style={{ color: 'var(--arora-text-muted)', fontSize: '13px' }}>
                      at <strong>{item.stepName}</strong>
                    </span>
                  )}
                  {item.sequence !== undefined && (
                    <span style={{ marginLeft: 'auto', fontSize: '11px', color: 'var(--arora-text-muted)' }}>
                      #{item.sequence}
                    </span>
                  )}
                </div>
                <div className="arora-timeline-time" style={{ fontSize: '12px', color: 'var(--arora-text-muted)', marginTop: '2px' }}>
                  {item.timestamp ? new Date(item.timestamp).toLocaleString() : ''}
                  {item.actor && ` by ${item.actor}`}
                </div>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
};
