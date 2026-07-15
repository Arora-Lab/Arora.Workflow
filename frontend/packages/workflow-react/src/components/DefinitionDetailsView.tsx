import React, { useState } from 'react';
import { useWorkflowDefinitionDetails } from '../hooks/useWorkflowDefinitionDetails';
import { WorkflowVisualizer } from './WorkflowVisualizer';

interface DefinitionDetailsViewProps {
  definitionId: string | null;
}

export const DefinitionDetailsView: React.FC<DefinitionDetailsViewProps> = ({ definitionId }) => {
  const { details, loading, error } = useWorkflowDefinitionDetails(definitionId);
  const [copied, setCopied] = useState(false);

  if (!definitionId) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state">
          <p>Select a definition from the list to view its visual graph and exporter options.</p>
        </div>
      </div>
    );
  }

  if (loading) {
    return <div className="arora-loading-spinner" />;
  }

  if (error || !details) {
    return (
      <div className="arora-card">
        <div className="arora-empty-state" style={{ borderColor: 'var(--arora-color-rejected)' }}>
          <p style={{ color: 'var(--arora-color-rejected)', fontWeight: 'bold' }}>Error loading definition details</p>
          <p style={{ fontSize: '13px' }}>{error?.message || 'Definition not found.'}</p>
        </div>
      </div>
    );
  }

  const handleCopyMermaid = () => {
    navigator.clipboard.writeText(details.mermaid || '');
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const getSeverityName = (severity: number | undefined) => {
    switch (severity) {
      case 0: return 'Error';
      case 1: return 'Warning';
      case 2: return 'Suggestion';
      case 3: return 'Info';
      default: return 'Unknown';
    }
  };

  const getSeverityBadgeClass = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'error':
        return 'arora-badge-rejected';
      case 'warning':
        return 'arora-badge-pendingapproval';
      case 'suggestion':
        return 'arora-badge-running';
      default:
        return 'arora-badge-cancelled';
    }
  };

  const diagnostics = details.diagnostics || [];
  const errors = diagnostics.filter((d) => d.severity === 0);
  const warnings = diagnostics.filter((d) => d.severity === 1);
  const suggestions = diagnostics.filter((d) => d.severity === 2 || d.severity === 3);

  return (
    <div className="arora-card" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      <h3 className="arora-card-title">
        <span>Definition Details: {details.name}</span>
        <span className="arora-badge arora-badge-completed">v{details.version}</span>
      </h3>

      {/* SVG Flowchart Graph */}
      <div>
        <span className="arora-detail-label" style={{ display: 'block', marginBottom: '8px' }}>
          Flowchart Diagram
        </span>
        <div style={{ height: '350px' }}>
          {details.layout && <WorkflowVisualizer layout={details.layout} />}
        </div>
      </div>

      {/* Diagnostics / Validation */}
      <div>
        <span className="arora-detail-label" style={{ display: 'block', marginBottom: '8px' }}>
          Diagnostics & Validation
        </span>
        {diagnostics.length === 0 ? (
          <div style={{ padding: '12px', background: 'rgba(16, 185, 129, 0.08)', border: '1px solid rgba(16, 185, 129, 0.2)', borderRadius: '8px', color: '#34d399', fontSize: '13px' }}>
            ✓ Definition is valid and clean. No warnings detected.
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '180px', overflowY: 'auto' }}>
            {diagnostics.map((diag, idx) => {
              const severityName = getSeverityName(diag.severity);
              return (
                <div 
                  key={`diag-${idx}`} 
                  style={{ 
                    padding: '10px 14px', 
                    background: 'rgba(30, 41, 59, 0.5)', 
                    border: '1px solid #334155', 
                    borderRadius: '8px', 
                    fontSize: '12px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '4px'
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontWeight: '700', color: '#f3f4f6' }}>{diag.code} {diag.nodeName && `[Node: ${diag.nodeName}]`}</span>
                    <span className={`arora-badge ${getSeverityBadgeClass(severityName)}`} style={{ fontSize: '10px', padding: '2px 6px' }}>
                      {severityName}
                    </span>
                  </div>
                  <div style={{ color: '#cbd5e1' }}>{diag.message}</div>
                  {diag.suggestion && (
                    <div style={{ color: '#a855f7', fontStyle: 'italic', marginTop: '2px' }}>
                      💡 Suggestion: {diag.suggestion}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Mermaid Export */}
      <div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
          <span className="arora-detail-label">Mermaid Exporter</span>
          <button 
            className="arora-btn arora-btn-secondary" 
            onClick={handleCopyMermaid}
            style={{ padding: '4px 8px', fontSize: '11px' }}
          >
            {copied ? 'Copied! ✓' : 'Copy Code'}
          </button>
        </div>
        <pre className="arora-code-panel" style={{ maxHeight: '150px', overflowY: 'auto', fontSize: '11px', margin: 0 }}>
          {details.mermaid || ''}
        </pre>
      </div>
    </div>
  );
};
