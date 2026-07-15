"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PendingApprovalsList = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
const usePendingApprovals_1 = require("../hooks/usePendingApprovals");
const PendingApprovalsList = ({ onSelectInstance }) => {
    const { approvals, loading, error, approve, reject } = (0, usePendingApprovals_1.usePendingApprovals)();
    const [submittingId, setSubmittingId] = (0, react_1.useState)(null);
    const [actionError, setActionError] = (0, react_1.useState)(null);
    const handleAction = async (id, type) => {
        setSubmittingId(id);
        setActionError(null);
        try {
            if (type === 'approve') {
                await approve(id);
            }
            else {
                await reject(id);
            }
        }
        catch (err) {
            setActionError(err?.message || `Failed to submit ${type} decision.`);
        }
        finally {
            setSubmittingId(null);
        }
    };
    if (loading && approvals.length === 0) {
        return (0, jsx_runtime_1.jsx)("div", { className: "arora-loading-spinner" });
    }
    if (error) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsxs)("div", { className: "arora-empty-state", style: { borderColor: 'var(--arora-color-rejected)' }, children: [(0, jsx_runtime_1.jsx)("p", { style: { color: 'var(--arora-color-rejected)', fontWeight: 'bold' }, children: "Error loading approvals" }), (0, jsx_runtime_1.jsx)("p", { style: { fontSize: '13px' }, children: error.message })] }) }));
    }
    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-card", children: [(0, jsx_runtime_1.jsxs)("h3", { className: "arora-card-title", children: [(0, jsx_runtime_1.jsx)("span", { children: "Pending Approvals" }), (0, jsx_runtime_1.jsxs)("span", { style: { fontSize: '12px', fontWeight: 'normal', color: 'var(--arora-text-muted)' }, children: ["Assigned to you: ", approvals.length] })] }), actionError && ((0, jsx_runtime_1.jsx)("div", { style: {
                    background: 'rgba(239, 68, 68, 0.1)',
                    border: '1px solid var(--arora-color-rejected)',
                    color: 'var(--arora-color-rejected)',
                    padding: '12px',
                    borderRadius: 'var(--arora-radius-sm)',
                    fontSize: '13px',
                }, children: actionError })), approvals.length === 0 ? ((0, jsx_runtime_1.jsxs)("div", { className: "arora-empty-state", children: [(0, jsx_runtime_1.jsxs)("svg", { width: "24", height: "24", viewBox: "0 0 24 24", fill: "none", stroke: "currentColor", strokeWidth: "2", strokeLinecap: "round", strokeLinejoin: "round", children: [(0, jsx_runtime_1.jsx)("circle", { cx: "12", cy: "12", r: "10" }), (0, jsx_runtime_1.jsx)("path", { d: "m9 12 2 2 4-4" })] }), (0, jsx_runtime_1.jsx)("p", { children: "You have no pending approvals. Nice job!" })] })) : ((0, jsx_runtime_1.jsx)("div", { className: "arora-approval-grid", children: approvals.map((app) => {
                    const isSubmitting = submittingId === app.approvalId;
                    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-approval-card", children: [(0, jsx_runtime_1.jsxs)("div", { className: "arora-approval-card-header", children: [(0, jsx_runtime_1.jsxs)("div", { children: [(0, jsx_runtime_1.jsx)("h4", { className: "arora-approval-title", children: app.workflowName }), (0, jsx_runtime_1.jsxs)("span", { className: "arora-approval-subtitle", children: ["Step: ", app.stepName] })] }), (0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-secondary", onClick: () => onSelectInstance?.(app.workflowInstanceId), style: { padding: '4px 8px', fontSize: '11px' }, children: "View Instance" })] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-approval-metadata", children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-approval-meta-label", children: "Correlation ID" }), (0, jsx_runtime_1.jsx)("span", { className: "arora-approval-meta-value", style: { fontFamily: 'monospace' }, children: app.correlationId }), (0, jsx_runtime_1.jsx)("span", { className: "arora-approval-meta-label", children: "Assigned" }), (0, jsx_runtime_1.jsx)("span", { className: "arora-approval-meta-value", children: new Date(app.createdAt).toLocaleDateString() }), app.deadlineAt && ((0, jsx_runtime_1.jsxs)(jsx_runtime_1.Fragment, { children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-approval-meta-label", style: { color: 'var(--arora-color-rejected)' }, children: "Deadline" }), (0, jsx_runtime_1.jsx)("span", { className: "arora-approval-meta-value", style: { color: 'var(--arora-color-rejected)' }, children: new Date(app.deadlineAt).toLocaleString() })] }))] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-approval-actions", children: [(0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-primary", disabled: isSubmitting, onClick: () => handleAction(app.approvalId, 'approve'), children: isSubmitting ? 'Approving...' : 'Approve' }), (0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-danger", disabled: isSubmitting, onClick: () => handleAction(app.approvalId, 'reject'), children: "Reject" })] })] }, app.approvalId));
                }) }))] }));
};
exports.PendingApprovalsList = PendingApprovalsList;
