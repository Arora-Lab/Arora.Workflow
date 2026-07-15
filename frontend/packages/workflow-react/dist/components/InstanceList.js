"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.InstanceList = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = __importDefault(require("react"));
const useWorkflowInstances_1 = require("../hooks/useWorkflowInstances");
const InstanceList = ({ onSelectInstance, selectedInstanceId, definitionIdFilter, }) => {
    const { instances, totalCount, loading, error, page, setPage, pageSize, status, setStatus, definitionId, setDefinitionId, } = (0, useWorkflowInstances_1.useWorkflowInstances)(1, 10, definitionIdFilter);
    react_1.default.useEffect(() => {
        if (definitionIdFilter !== undefined) {
            setDefinitionId(definitionIdFilter);
        }
    }, [definitionIdFilter, setDefinitionId]);
    if (loading && instances.length === 0) {
        return (0, jsx_runtime_1.jsx)("div", { className: "arora-loading-spinner" });
    }
    if (error) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsxs)("div", { className: "arora-empty-state", style: { borderColor: 'var(--arora-color-rejected)' }, children: [(0, jsx_runtime_1.jsx)("p", { style: { color: 'var(--arora-color-rejected)', fontWeight: 'bold' }, children: "Error loading instances" }), (0, jsx_runtime_1.jsx)("p", { style: { fontSize: '13px' }, children: error.message })] }) }));
    }
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
            case 'pending_approval':
                return 'arora-badge-pendingapproval';
            default:
                return '';
        }
    };
    const formatDuration = (startStr, endStr) => {
        const start = new Date(startStr).getTime();
        const end = endStr ? new Date(endStr).getTime() : Date.now();
        const diffMs = end - start;
        if (diffMs < 0)
            return '0s';
        const diffSecs = Math.floor(diffMs / 1000);
        const diffMins = Math.floor(diffSecs / 60);
        const diffHours = Math.floor(diffMins / 60);
        if (diffHours > 0)
            return `${diffHours}h ${diffMins % 60}m`;
        if (diffMins > 0)
            return `${diffMins}m ${diffSecs % 60}s`;
        return `${diffSecs}s`;
    };
    const totalPages = Math.ceil(totalCount / pageSize);
    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-card", children: [(0, jsx_runtime_1.jsxs)("h3", { className: "arora-card-title", children: [(0, jsx_runtime_1.jsx)("span", { children: "Workflow Instances" }), (0, jsx_runtime_1.jsxs)("span", { style: { fontSize: '12px', fontWeight: 'normal', color: 'var(--arora-text-muted)' }, children: ["Total: ", totalCount] })] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-filters-bar", children: [(0, jsx_runtime_1.jsxs)("div", { style: { display: 'flex', gap: '8px', alignItems: 'center' }, children: [(0, jsx_runtime_1.jsx)("label", { style: { fontSize: '12px', color: 'var(--arora-text-muted)' }, children: "Status:" }), (0, jsx_runtime_1.jsxs)("select", { className: "arora-select", value: status || '', onChange: (e) => setStatus(e.target.value || undefined), children: [(0, jsx_runtime_1.jsx)("option", { value: "", children: "All Statuses" }), (0, jsx_runtime_1.jsx)("option", { value: "Running", children: "Running" }), (0, jsx_runtime_1.jsx)("option", { value: "PendingApproval", children: "Pending Approval" }), (0, jsx_runtime_1.jsx)("option", { value: "Completed", children: "Completed" }), (0, jsx_runtime_1.jsx)("option", { value: "Rejected", children: "Rejected" }), (0, jsx_runtime_1.jsx)("option", { value: "Cancelled", children: "Cancelled" })] })] }), !definitionIdFilter && ((0, jsx_runtime_1.jsxs)("div", { style: { display: 'flex', gap: '8px', alignItems: 'center' }, children: [(0, jsx_runtime_1.jsx)("label", { style: { fontSize: '12px', color: 'var(--arora-text-muted)' }, children: "Definition ID:" }), (0, jsx_runtime_1.jsx)("input", { type: "text", className: "arora-input", placeholder: "Filter by ID...", value: definitionId || '', onChange: (e) => setDefinitionId(e.target.value || undefined) })] }))] }), instances.length === 0 ? ((0, jsx_runtime_1.jsx)("div", { className: "arora-empty-state", children: (0, jsx_runtime_1.jsx)("p", { children: "No workflow instances found matching the filters." }) })) : ((0, jsx_runtime_1.jsxs)(jsx_runtime_1.Fragment, { children: [(0, jsx_runtime_1.jsx)("div", { className: "arora-table-wrapper", children: (0, jsx_runtime_1.jsxs)("table", { className: "arora-table", children: [(0, jsx_runtime_1.jsx)("thead", { children: (0, jsx_runtime_1.jsxs)("tr", { children: [(0, jsx_runtime_1.jsx)("th", { children: "Instance ID" }), (0, jsx_runtime_1.jsx)("th", { children: "Definition Version" }), (0, jsx_runtime_1.jsx)("th", { children: "Current State" }), (0, jsx_runtime_1.jsx)("th", { children: "Status" }), (0, jsx_runtime_1.jsx)("th", { children: "Duration" }), (0, jsx_runtime_1.jsx)("th", { children: "Created" })] }) }), (0, jsx_runtime_1.jsx)("tbody", { children: instances.map((inst) => {
                                        const instId = inst.id || '';
                                        if (!instId)
                                            return null;
                                        const isActive = instId === selectedInstanceId;
                                        const status = inst.status || 'Unknown';
                                        const currentState = inst.currentState || 'None';
                                        const createdAt = inst.createdAt || '';
                                        const modifiedAt = inst.modifiedAt || null;
                                        return ((0, jsx_runtime_1.jsxs)("tr", { onClick: () => onSelectInstance?.(instId), style: isActive ? { backgroundColor: 'var(--arora-primary-light)' } : undefined, children: [(0, jsx_runtime_1.jsxs)("td", { style: { fontFamily: 'monospace', fontSize: '12px', fontWeight: 'bold' }, children: [instId.substring(0, 8), "..."] }), (0, jsx_runtime_1.jsxs)("td", { children: ["v", inst.workflowDefinitionVersion] }), (0, jsx_runtime_1.jsx)("td", { children: (0, jsx_runtime_1.jsx)("span", { style: { fontWeight: '600' }, children: currentState }) }), (0, jsx_runtime_1.jsx)("td", { children: (0, jsx_runtime_1.jsx)("span", { className: `arora-badge ${getStatusBadgeClass(status)}`, children: status }) }), (0, jsx_runtime_1.jsx)("td", { children: formatDuration(createdAt, status.toLowerCase() !== 'running' && status.toLowerCase() !== 'pendingapproval' ? modifiedAt : null) }), (0, jsx_runtime_1.jsx)("td", { children: createdAt ? new Date(createdAt).toLocaleDateString() : '' })] }, instId));
                                    }) })] }) }), totalPages > 1 && ((0, jsx_runtime_1.jsxs)("div", { className: "arora-pagination", children: [(0, jsx_runtime_1.jsxs)("span", { className: "arora-pagination-info", children: ["Page ", page, " of ", totalPages] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-pagination-controls", children: [(0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-secondary", disabled: page <= 1, onClick: () => setPage(page - 1), style: { padding: '6px 12px', fontSize: '12px' }, children: "Previous" }), (0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-secondary", disabled: page >= totalPages, onClick: () => setPage(page + 1), style: { padding: '6px 12px', fontSize: '12px' }, children: "Next" })] })] }))] }))] }));
};
exports.InstanceList = InstanceList;
