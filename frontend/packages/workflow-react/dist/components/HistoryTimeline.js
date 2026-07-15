"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.HistoryTimeline = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const useWorkflowInstanceHistory_1 = require("../hooks/useWorkflowInstanceHistory");
const HistoryTimeline = ({ instanceId }) => {
    const { history, loading, error } = (0, useWorkflowInstanceHistory_1.useWorkflowInstanceHistory)(instanceId);
    if (!instanceId) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsx)("div", { className: "arora-empty-state", children: (0, jsx_runtime_1.jsx)("p", { children: "Select a workflow instance to view its execution history." }) }) }));
    }
    if (loading && history.length === 0) {
        return (0, jsx_runtime_1.jsx)("div", { className: "arora-loading-spinner" });
    }
    if (error) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsxs)("div", { className: "arora-empty-state", style: { borderColor: 'var(--arora-color-rejected)' }, children: [(0, jsx_runtime_1.jsx)("p", { style: { color: 'var(--arora-color-rejected)', fontWeight: 'bold' }, children: "Error loading history" }), (0, jsx_runtime_1.jsx)("p", { style: { fontSize: '13px' }, children: error.message })] }) }));
    }
    if (history.length === 0) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsx)("div", { className: "arora-empty-state", children: (0, jsx_runtime_1.jsx)("p", { children: "No history records found for this instance." }) }) }));
    }
    const getNodeColor = (action) => {
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
    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-card", children: [(0, jsx_runtime_1.jsx)("h3", { className: "arora-card-title", children: "Execution History" }), (0, jsx_runtime_1.jsx)("div", { className: "arora-timeline", children: history.map((item) => ((0, jsx_runtime_1.jsxs)("div", { className: "arora-timeline-item", children: [(0, jsx_runtime_1.jsx)("div", { className: "arora-timeline-node", style: {
                                backgroundColor: getNodeColor(item.action),
                                boxShadow: `0 0 8px ${getNodeColor(item.action)}`,
                            } }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-timeline-content", children: [(0, jsx_runtime_1.jsxs)("div", { className: "arora-timeline-header", children: [(0, jsx_runtime_1.jsx)("span", { style: { fontWeight: '700', fontSize: '14px' }, children: item.action }), item.stepName && ((0, jsx_runtime_1.jsxs)("span", { style: { color: 'var(--arora-text-muted)', fontSize: '13px' }, children: ["at ", (0, jsx_runtime_1.jsx)("strong", { children: item.stepName })] }))] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-timeline-time", children: [new Date(item.timestamp).toLocaleString(), item.actor && ` by ${item.actor}`] })] })] }, item.id))) })] }));
};
exports.HistoryTimeline = HistoryTimeline;
