import React, { useState, useEffect } from 'react';
import { useWorkflowInstanceDetails } from '../hooks/useWorkflowInstanceDetails';
import { useWorkflowDefinitionDetails } from '../hooks/useWorkflowDefinitionDetails';
import { useWorkflowInstanceHistory } from '../hooks/useWorkflowInstanceHistory';
import { WorkflowVisualizer } from './WorkflowVisualizer';
import { HistoryTimeline } from './HistoryTimeline';
import { derivePlaybackSnapshot } from '@arora/workflow-visualization';

export interface InstanceDetailsViewProps {
  instanceId: string;
}

export const InstanceDetailsView: React.FC<InstanceDetailsViewProps> = ({ instanceId }) => {
  const { instance, loading, error } = useWorkflowInstanceDetails(instanceId);
  const { details, loading: detailsLoading } = useWorkflowDefinitionDetails(instance?.workflowDefinitionId ?? null);
  const { history } = useWorkflowInstanceHistory(instanceId);
  const [selectedSequence, setSelectedSequence] = useState<number | null>(null);

  useEffect(() => {
    if (history && history.length > 0) {
      const maxSeq = Math.max(...history.map((h) => h.sequence ?? 0));
      setSelectedSequence(maxSeq);
    } else {
      setSelectedSequence(null);
    }
  }, [history]);

  const playback = (details?.layout && selectedSequence !== null)
    ? derivePlaybackSnapshot(history as any, selectedSequence, details.layout as any)
    : null;

  if (!instanceId) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state">
          <p>Select a workflow instance to view details.</p>
        </div>
      </div>
    );
  }

  if (loading) {
    return <div className="arora-loading-spinner" />;
  }

  if (error || !instance) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state" style={{ borderColor: 'var(--arora-color-rejected)' }}>
          <p style={{ color: 'var(--arora-color-rejected)', fontWeight: 'bold' }}>Error loading details</p>
          <p style={{ fontSize: '13px' }}>{error?.message || 'Instance not found.'}</p>
        </div>
      </div>
    );
  }

  const formatJson = (jsonStr: string | null) => {
    if (!jsonStr) return 'No Input Parameters';
    try {
      const parsed = JSON.parse(jsonStr);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return jsonStr;
    }
  };

  const getStatusBadgeClass = (statusStr: string) => {
    switch (statusStr.toLowerCase()) {
      case 'running':
        return 'arora-badge-running';
      case 'completed':
        return 'arora-badge-completed';
      case 'rejected':
        return 'arora-badge-rejected';
      case 'cancelled':
        return 'arora-badge-cancelled';
      case 'pendingapproval':
        return 'arora-badge-pendingapproval';
      default:
        return '';
    }
  };

  return (
    <div className="arora-card">
      <h3 className="arora-card-title">
        <span>Instance Details</span>
        <span className={`arora-badge ${getStatusBadgeClass(instance.status || '')}`}>
          {instance.status || 'Unknown'}
        </span>
      </h3>

      <div className="arora-detail-grid">
        <div className="arora-detail-item">
          <span className="arora-detail-label">Instance ID</span>
          <span className="arora-detail-value" style={{ fontFamily: 'monospace', fontSize: '12px' }}>
            {instance.id}
          </span>
        </div>

        <div className="arora-detail-item">
          <span className="arora-detail-label">Definition ID</span>
          <span className="arora-detail-value" style={{ fontFamily: 'monospace', fontSize: '12px' }}>
            {instance.workflowDefinitionId}
          </span>
        </div>

        <div className="arora-detail-item">
          <span className="arora-detail-label">Definition Version</span>
          <span className="arora-detail-value">v{instance.workflowDefinitionVersion}</span>
        </div>

        <div className="arora-detail-item">
          <span className="arora-detail-label">Current State</span>
          <span className="arora-detail-value" style={{ color: 'var(--arora-primary)' }}>
            {instance.currentState}
          </span>
        </div>

        <div className="arora-detail-item">
          <span className="arora-detail-label">Created At</span>
          <span className="arora-detail-value">
            {instance.createdAt ? new Date(instance.createdAt).toLocaleString() : ''}
          </span>
        </div>

        <div className="arora-detail-item">
          <span className="arora-detail-label">Last Modified At</span>
          <span className="arora-detail-value">
            {instance.modifiedAt ? new Date(instance.modifiedAt).toLocaleString() : ''}
          </span>
        </div>
      </div>

      {details?.layout && (
        <div style={{ marginTop: '20px', marginBottom: '20px' }}>
          <span className="arora-detail-label" style={{ display: 'block', marginBottom: '6px' }}>
            Workflow Graph Visualizer (Time-Travel Playback)
          </span>
          <div style={{ height: '350px' }}>
            <WorkflowVisualizer
              layout={details.layout}
              activeNodeName={instance.currentState}
              nodeStates={playback?.nodeStates as any}
              completedConnectionIds={playback?.completedConnectionIds}
            />
          </div>
        </div>
      )}

      {selectedSequence !== null && (
        <div style={{ marginTop: '20px', marginBottom: '20px' }}>
          <HistoryTimeline
            instanceId={instanceId}
            historyItems={history}
            selectedSequence={selectedSequence}
            onSelectSequence={setSelectedSequence}
          />
        </div>
      )}

      <div style={{ marginTop: '10px' }}>
        <span className="arora-detail-label" style={{ display: 'block', marginBottom: '6px' }}>
          Input Parameters (JSON)
        </span>
        <pre className="arora-code-panel">{formatJson(instance.inputJson ?? null)}</pre>
      </div>
    </div>
  );
};
