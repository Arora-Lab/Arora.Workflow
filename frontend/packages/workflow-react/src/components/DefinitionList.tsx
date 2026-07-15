import React from 'react';
import { useWorkflowDefinitions } from '../hooks/useWorkflowDefinitions';

export interface DefinitionListProps {
  onSelectDefinition?: (definitionId: string) => void;
  selectedDefinitionId?: string;
}

export const DefinitionList: React.FC<DefinitionListProps> = ({
  onSelectDefinition,
  selectedDefinitionId,
}) => {
  const { definitions, totalCount, loading, error, page, setPage, pageSize } = useWorkflowDefinitions();

  if (loading && definitions.length === 0) {
    return <div className="arora-loading-spinner" />;
  }

  if (error) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state" style={{ borderColor: 'var(--arora-color-rejected)' }}>
          <p style={{ color: 'var(--arora-color-rejected)', fontWeight: 'bold' }}>Error loading definitions</p>
          <p style={{ fontSize: '13px' }}>{error.message}</p>
        </div>
      </div>
    );
  }

  if (definitions.length === 0) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state">
          <p>No workflow definitions found.</p>
        </div>
      </div>
    );
  }

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="arora-card">
      <h3 className="arora-card-title">
        <span>Workflow Definitions</span>
        <span style={{ fontSize: '12px', fontWeight: 'normal', color: 'var(--arora-text-muted)' }}>
          Total: {totalCount}
        </span>
      </h3>

      <div className="arora-table-wrapper">
        <table className="arora-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Version</th>
              <th>Steps</th>
              <th>Created At</th>
            </tr>
          </thead>
          <tbody>
            {definitions.map((def) => {
              const defId = def.id || '';
              if (!defId) return null;

              const isActive = defId === selectedDefinitionId;
              return (
                <tr
                  key={defId}
                  onClick={() => onSelectDefinition?.(defId)}
                  style={isActive ? { backgroundColor: 'var(--arora-primary-light)' } : undefined}
                >
                  <td style={{ fontWeight: '600' }}>{def.name || 'Unnamed'}</td>
                  <td>v{def.version}</td>
                  <td>{def.stepCount}</td>
                  <td>{def.createdAt ? new Date(def.createdAt).toLocaleDateString() : ''}</td>
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
    </div>
  );
};
