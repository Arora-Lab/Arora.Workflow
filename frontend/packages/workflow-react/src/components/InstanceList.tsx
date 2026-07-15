import React from 'react';
import { useWorkflowInstances } from '../hooks/useWorkflowInstances';

export interface InstanceListProps {
  onSelectInstance?: (instanceId: string) => void;
  selectedInstanceId?: string;
  definitionIdFilter?: string;
}

export const InstanceList: React.FC<InstanceListProps> = ({
  onSelectInstance,
  selectedInstanceId,
  definitionIdFilter,
}) => {
  const {
    instances,
    totalCount,
    loading,
    error,
    page,
    setPage,
    pageSize,
    status,
    setStatus,
    definitionId,
    setDefinitionId,
  } = useWorkflowInstances(1, 10, definitionIdFilter);

  React.useEffect(() => {
    if (definitionIdFilter !== undefined) {
      setDefinitionId(definitionIdFilter);
    }
  }, [definitionIdFilter, setDefinitionId]);

  if (loading && instances.length === 0) {
    return <div className="arora-loading-spinner" />;
  }

  if (error) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state" style={{ borderColor: 'var(--arora-color-rejected)' }}>
          <p style={{ color: 'var(--arora-color-rejected)', fontWeight: 'bold' }}>Error loading instances</p>
          <p style={{ fontSize: '13px' }}>{error.message}</p>
        </div>
      </div>
    );
  }

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
      case 'pending_approval':
        return 'arora-badge-pendingapproval';
      default:
        return '';
    }
  };

  const formatDuration = (startStr: string, endStr?: string | null) => {
    const start = new Date(startStr).getTime();
    const end = endStr ? new Date(endStr).getTime() : Date.now();
    const diffMs = end - start;
    if (diffMs < 0) return '0s';
    
    const diffSecs = Math.floor(diffMs / 1000);
    const diffMins = Math.floor(diffSecs / 60);
    const diffHours = Math.floor(diffMins / 60);

    if (diffHours > 0) return `${diffHours}h ${diffMins % 60}m`;
    if (diffMins > 0) return `${diffMins}m ${diffSecs % 60}s`;
    return `${diffSecs}s`;
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="arora-card">
      <h3 className="arora-card-title">
        <span>Workflow Instances</span>
        <span style={{ fontSize: '12px', fontWeight: 'normal', color: 'var(--arora-text-muted)' }}>
          Total: {totalCount}
        </span>
      </h3>

      <div className="arora-filters-bar">
        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          <label style={{ fontSize: '12px', color: 'var(--arora-text-muted)' }}>Status:</label>
          <select
            className="arora-select"
            value={status || ''}
            onChange={(e) => setStatus(e.target.value || undefined)}
          >
            <option value="">All Statuses</option>
            <option value="Running">Running</option>
            <option value="PendingApproval">Pending Approval</option>
            <option value="Completed">Completed</option>
            <option value="Rejected">Rejected</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        {!definitionIdFilter && (
          <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            <label style={{ fontSize: '12px', color: 'var(--arora-text-muted)' }}>Definition ID:</label>
            <input
              type="text"
              className="arora-input"
              placeholder="Filter by ID..."
              value={definitionId || ''}
              onChange={(e) => setDefinitionId(e.target.value || undefined)}
            />
          </div>
        )}
      </div>

      {instances.length === 0 ? (
        <div className="arora-empty-state">
          <p>No workflow instances found matching the filters.</p>
        </div>
      ) : (
        <>
          <div className="arora-table-wrapper">
            <table className="arora-table">
              <thead>
                <tr>
                  <th>Instance ID</th>
                  <th>Definition Version</th>
                  <th>Current State</th>
                  <th>Status</th>
                  <th>Duration</th>
                  <th>Created</th>
                </tr>
              </thead>
              <tbody>
                {instances.map((inst) => {
                  const isActive = inst.id === selectedInstanceId;
                  return (
                    <tr
                      key={inst.id}
                      onClick={() => onSelectInstance?.(inst.id)}
                      style={isActive ? { backgroundColor: 'var(--arora-primary-light)' } : undefined}
                    >
                      <td style={{ fontFamily: 'monospace', fontSize: '12px', fontWeight: 'bold' }}>
                        {inst.id.substring(0, 8)}...
                      </td>
                      <td>v{inst.workflowDefinitionVersion}</td>
                      <td>
                        <span style={{ fontWeight: '600' }}>{inst.currentState}</span>
                      </td>
                      <td>
                        <span className={`arora-badge ${getStatusBadgeClass(inst.status)}`}>
                          {inst.status}
                        </span>
                      </td>
                      <td>{formatDuration(inst.createdAt, inst.status.toLowerCase() !== 'running' && inst.status.toLowerCase() !== 'pendingapproval' ? inst.modifiedAt : null)}</td>
                      <td>{new Date(inst.createdAt).toLocaleDateString()}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className="arora-pagination">
              <span className="arora-pagination-info">
                Page {page} of {totalPages}
              </span>
              <div className="arora-pagination-controls">
                <button
                  className="arora-btn arora-btn-secondary"
                  disabled={page <= 1}
                  onClick={() => setPage(page - 1)}
                  style={{ padding: '6px 12px', fontSize: '12px' }}
                >
                  Previous
                </button>
                <button
                  className="arora-btn arora-btn-secondary"
                  disabled={page >= totalPages}
                  onClick={() => setPage(page + 1)}
                  style={{ padding: '6px 12px', fontSize: '12px' }}
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
};
