"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.InstanceDetailsView = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
const useWorkflowInstanceDetails_1 = require("../hooks/useWorkflowInstanceDetails");
const useWorkflowDefinitionDetails_1 = require("../hooks/useWorkflowDefinitionDetails");
const useWorkflowInstanceHistory_1 = require("../hooks/useWorkflowInstanceHistory");
const WorkflowVisualizer_1 = require("./WorkflowVisualizer");
const HistoryTimeline_1 = require("./HistoryTimeline");
const workflow_visualization_1 = require("@arora/workflow-visualization");
const InstanceDetailsView = ({ instanceId }) => {
    const { instance, loading, error } = (0, useWorkflowInstanceDetails_1.useWorkflowInstanceDetails)(instanceId);
    const { details, loading: detailsLoading } = (0, useWorkflowDefinitionDetails_1.useWorkflowDefinitionDetails)(instance?.workflowDefinitionId ?? null);
    const { history } = (0, useWorkflowInstanceHistory_1.useWorkflowInstanceHistory)(instanceId);
    const [selectedSequence, setSelectedSequence] = (0, react_1.useState)(null);
    (0, react_1.useEffect)(() => {
        if (history && history.length > 0) {
            const maxSeq = Math.max(...history.map((h) => h.sequence ?? 0));
            setSelectedSequence(maxSeq);
        }
        else {
            setSelectedSequence(null);
        }
    }, [history]);
    const playback = (details?.layout && selectedSequence !== null)
        ? (0, workflow_visualization_1.derivePlaybackSnapshot)(history, selectedSequence, details.layout)
        : null;
    if (!instanceId) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsx)("div", { className: "arora-empty-state", children: (0, jsx_runtime_1.jsx)("p", { children: "Select a workflow instance to view details." }) }) }));
    }
    if (loading) {
        return (0, jsx_runtime_1.jsx)("div", { className: "arora-loading-spinner" });
    }
    if (error || !instance) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsxs)("div", { className: "arora-empty-state", style: { borderColor: 'var(--arora-color-rejected)' }, children: [(0, jsx_runtime_1.jsx)("p", { style: { color: 'var(--arora-color-rejected)', fontWeight: 'bold' }, children: "Error loading details" }), (0, jsx_runtime_1.jsx)("p", { style: { fontSize: '13px' }, children: error?.message || 'Instance not found.' })] }) }));
    }
    const formatJson = (jsonStr) => {
        if (!jsonStr)
            return 'No Input Parameters';
        try {
            const parsed = JSON.parse(jsonStr);
            return JSON.stringify(parsed, null, 2);
        }
        catch {
            return jsonStr;
        }
    };
    const getStatusBadgeClass = (statusStr) => {
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
    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-card", children: [(0, jsx_runtime_1.jsxs)("h3", { className: "arora-card-title", children: [(0, jsx_runtime_1.jsx)("span", { children: "Instance Details" }), (0, jsx_runtime_1.jsx)("span", { className: `arora-badge ${getStatusBadgeClass(instance.status || '')}`, children: instance.status || 'Unknown' })] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-detail-grid", children: [(0, jsx_runtime_1.jsxs)("div", { className: "arora-detail-item", children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", children: "Instance ID" }), (0, jsx_runtime_1.jsx)("span", { className: "arora-detail-value", style: { fontFamily: 'monospace', fontSize: '12px' }, children: instance.id })] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-detail-item", children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", children: "Definition ID" }), (0, jsx_runtime_1.jsx)("span", { className: "arora-detail-value", style: { fontFamily: 'monospace', fontSize: '12px' }, children: instance.workflowDefinitionId })] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-detail-item", children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", children: "Definition Version" }), (0, jsx_runtime_1.jsxs)("span", { className: "arora-detail-value", children: ["v", instance.workflowDefinitionVersion] })] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-detail-item", children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", children: "Current State" }), (0, jsx_runtime_1.jsx)("span", { className: "arora-detail-value", style: { color: 'var(--arora-primary)' }, children: instance.currentState })] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-detail-item", children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", children: "Created At" }), (0, jsx_runtime_1.jsx)("span", { className: "arora-detail-value", children: instance.createdAt ? new Date(instance.createdAt).toLocaleString() : '' })] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-detail-item", children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", children: "Last Modified At" }), (0, jsx_runtime_1.jsx)("span", { className: "arora-detail-value", children: instance.modifiedAt ? new Date(instance.modifiedAt).toLocaleString() : '' })] })] }), details?.layout && ((0, jsx_runtime_1.jsxs)("div", { style: { marginTop: '20px', marginBottom: '20px' }, children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", style: { display: 'block', marginBottom: '6px' }, children: "Workflow Graph Visualizer (Time-Travel Playback)" }), (0, jsx_runtime_1.jsx)("div", { style: { height: '350px' }, children: (0, jsx_runtime_1.jsx)(WorkflowVisualizer_1.WorkflowVisualizer, { layout: details.layout, activeNodeName: instance.currentState, nodeStates: playback?.nodeStates, completedConnectionIds: playback?.completedConnectionIds }) })] })), selectedSequence !== null && ((0, jsx_runtime_1.jsx)("div", { style: { marginTop: '20px', marginBottom: '20px' }, children: (0, jsx_runtime_1.jsx)(HistoryTimeline_1.HistoryTimeline, { instanceId: instanceId, historyItems: history, selectedSequence: selectedSequence, onSelectSequence: setSelectedSequence }) })), (0, jsx_runtime_1.jsxs)("div", { style: { marginTop: '10px' }, children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", style: { display: 'block', marginBottom: '6px' }, children: "Input Parameters (JSON)" }), (0, jsx_runtime_1.jsx)("pre", { className: "arora-code-panel", children: formatJson(instance.inputJson ?? null) })] })] }));
};
exports.InstanceDetailsView = InstanceDetailsView;
