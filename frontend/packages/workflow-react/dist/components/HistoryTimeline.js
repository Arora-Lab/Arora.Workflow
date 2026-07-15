"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.HistoryTimeline = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const useWorkflowInstanceHistory_1 = require("../hooks/useWorkflowInstanceHistory");
const HistoryTimeline = ({ instanceId, historyItems, selectedSequence, onSelectSequence, }) => {
    const { history: fetchedHistory, loading, error } = (0, useWorkflowInstanceHistory_1.useWorkflowInstanceHistory)(historyItems ? '' : instanceId);
    const history = historyItems || fetchedHistory;
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
    const handleKeyDown = (e, index) => {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            const nextBtn = document.getElementById(`timeline-btn-${index + 1}`);
            if (nextBtn) {
                nextBtn.focus();
                nextBtn.click();
            }
        }
        else if (e.key === 'ArrowUp') {
            e.preventDefault();
            const prevBtn = document.getElementById(`timeline-btn-${index - 1}`);
            if (prevBtn) {
                prevBtn.focus();
                prevBtn.click();
            }
        }
    };
    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-card", children: [(0, jsx_runtime_1.jsx)("h3", { className: "arora-card-title", children: "Execution History" }), (0, jsx_runtime_1.jsx)("div", { className: "arora-timeline", style: { display: 'flex', flexDirection: 'column', gap: '8px' }, children: history.map((item, idx) => {
                    const isSelected = selectedSequence === item.sequence;
                    return ((0, jsx_runtime_1.jsxs)("button", { id: `timeline-btn-${idx}`, className: `arora-timeline-item-btn ${isSelected ? 'active' : ''}`, onClick: () => onSelectSequence?.(item.sequence ?? 0), onKeyDown: (e) => handleKeyDown(e, idx), style: {
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
                        }, children: [(0, jsx_runtime_1.jsx)("div", { className: "arora-timeline-node", style: {
                                    backgroundColor: getNodeColor(item.action || ''),
                                    boxShadow: `0 0 8px ${getNodeColor(item.action || '')}`,
                                    marginRight: '12px',
                                    width: '12px',
                                    height: '12px',
                                    borderRadius: '50%',
                                    flexShrink: 0
                                } }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-timeline-content", style: { flexGrow: 1 }, children: [(0, jsx_runtime_1.jsxs)("div", { className: "arora-timeline-header", style: { display: 'flex', gap: '8px', alignItems: 'baseline' }, children: [(0, jsx_runtime_1.jsx)("span", { style: { fontWeight: '700', fontSize: '14px' }, children: item.action || 'Unknown Action' }), item.stepName && ((0, jsx_runtime_1.jsxs)("span", { style: { color: 'var(--arora-text-muted)', fontSize: '13px' }, children: ["at ", (0, jsx_runtime_1.jsx)("strong", { children: item.stepName })] })), item.sequence !== undefined && ((0, jsx_runtime_1.jsxs)("span", { style: { marginLeft: 'auto', fontSize: '11px', color: 'var(--arora-text-muted)' }, children: ["#", item.sequence] }))] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-timeline-time", style: { fontSize: '12px', color: 'var(--arora-text-muted)', marginTop: '2px' }, children: [item.timestamp ? new Date(item.timestamp).toLocaleString() : '', item.actor && ` by ${item.actor}`] })] })] }, item.id));
                }) })] }));
};
exports.HistoryTimeline = HistoryTimeline;
