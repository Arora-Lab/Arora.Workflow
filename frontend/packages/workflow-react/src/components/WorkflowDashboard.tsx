import React, { useState } from 'react';
import { DefinitionList } from './DefinitionList';
import { InstanceList } from './InstanceList';
import { InstanceDetailsView } from './InstanceDetailsView';
import { HistoryTimeline } from './HistoryTimeline';
import { PendingApprovalsList } from './PendingApprovalsList';
import { useAroraWorkflowContext } from '../context/AroraWorkflowContext';

export const WorkflowDashboard: React.FC = () => {
  const { currentUser } = useAroraWorkflowContext();
  const [activeTab, setActiveTab] = useState<'instances' | 'definitions' | 'approvals'>('instances');
  const [selectedInstanceId, setSelectedInstanceId] = useState<string | null>(null);
  const [selectedDefinitionId, setSelectedDefinitionId] = useState<string | null>(null);
  const [isLightTheme, setIsLightTheme] = useState(false);

  const handleSelectInstance = (id: string) => {
    setSelectedInstanceId(id);
  };

  const handleSelectDefinition = (id: string) => {
    setSelectedDefinitionId(id);
    setActiveTab('instances');
  };

  const toggleTheme = () => {
    setIsLightTheme((prev) => !prev);
  };

  return (
    <div className={`arora-dashboard-container ${isLightTheme ? 'arora-light-theme' : ''}`}>
      <header className="arora-header">
        <div className="arora-title-group">
          <h1>Arora Workflow</h1>
          <p>Enterprise Approval Engine Management Dashboard</p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <button
            className="arora-btn arora-btn-secondary"
            onClick={toggleTheme}
            style={{ padding: '6px 12px', fontSize: '13px' }}
          >
            {isLightTheme ? 'Dark Mode 🌙' : 'Light Mode ☀️'}
          </button>

          <div className="arora-user-badge">
            <div className="avatar">{currentUser.substring(0, 2).toUpperCase()}</div>
            <span>Logged in as: <strong>{currentUser}</strong></span>
          </div>
        </div>
      </header>

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div className="arora-tabs">
          <button
            className={`arora-tab-button ${activeTab === 'instances' ? 'active' : ''}`}
            onClick={() => setActiveTab('instances')}
          >
            Instances
          </button>
          <button
            className={`arora-tab-button ${activeTab === 'definitions' ? 'active' : ''}`}
            onClick={() => setActiveTab('definitions')}
          >
            Definitions
          </button>
          <button
            className={`arora-tab-button ${activeTab === 'approvals' ? 'active' : ''}`}
            onClick={() => setActiveTab('approvals')}
          >
            My Approvals
          </button>
        </div>
      </div>

      <main style={{ minHeight: '500px' }}>
        {activeTab === 'instances' && (
          <div className="arora-layout-grid">
            <InstanceList
              onSelectInstance={handleSelectInstance}
              selectedInstanceId={selectedInstanceId || undefined}
              definitionIdFilter={selectedDefinitionId || undefined}
            />
            <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
              <InstanceDetailsView instanceId={selectedInstanceId || ''} />
              {selectedInstanceId && <HistoryTimeline instanceId={selectedInstanceId} />}
            </div>
          </div>
        )}

        {activeTab === 'definitions' && (
          <DefinitionList
            onSelectDefinition={handleSelectDefinition}
            selectedDefinitionId={selectedDefinitionId || undefined}
          />
        )}

        {activeTab === 'approvals' && (
          <PendingApprovalsList
            onSelectInstance={(id) => {
              setSelectedInstanceId(id);
              setActiveTab('instances');
            }}
          />
        )}
      </main>
    </div>
  );
};
