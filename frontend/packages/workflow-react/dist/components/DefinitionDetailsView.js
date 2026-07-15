"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DefinitionDetailsView = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
const useWorkflowDefinitionDetails_1 = require("../hooks/useWorkflowDefinitionDetails");
const WorkflowVisualizer_1 = require("./WorkflowVisualizer");
const DefinitionDetailsView = ({ definitionId }) => {
    const { details, loading, error } = (0, useWorkflowDefinitionDetails_1.useWorkflowDefinitionDetails)(definitionId);
    const [copied, setCopied] = (0, react_1.useState)(false);
    if (!definitionId) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsx)("div", { className: "arora-empty-state", children: (0, jsx_runtime_1.jsx)("p", { children: "Select a definition from the list to view its visual graph and exporter options." }) }) }));
    }
    if (loading) {
        return (0, jsx_runtime_1.jsx)("div", { className: "arora-loading-spinner" });
    }
    if (error || !details) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsxs)("div", { className: "arora-empty-state", style: { borderColor: 'var(--arora-color-rejected)' }, children: [(0, jsx_runtime_1.jsx)("p", { style: { color: 'var(--arora-color-rejected)', fontWeight: 'bold' }, children: "Error loading definition details" }), (0, jsx_runtime_1.jsx)("p", { style: { fontSize: '13px' }, children: error?.message || 'Definition not found.' })] }) }));
    }
    const handleCopyMermaid = () => {
        navigator.clipboard.writeText(details.mermaid || '');
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };
    const getSeverityName = (severity) => {
        switch (severity) {
            case 0: return 'Error';
            case 1: return 'Warning';
            case 2: return 'Suggestion';
            case 3: return 'Info';
            default: return 'Unknown';
        }
    };
    const getSeverityBadgeClass = (severity) => {
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
    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-card", style: { display: 'flex', flexDirection: 'column', gap: '20px' }, children: [(0, jsx_runtime_1.jsxs)("h3", { className: "arora-card-title", children: [(0, jsx_runtime_1.jsxs)("span", { children: ["Definition Details: ", details.name] }), (0, jsx_runtime_1.jsxs)("span", { className: "arora-badge arora-badge-completed", children: ["v", details.version] })] }), (0, jsx_runtime_1.jsxs)("div", { children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", style: { display: 'block', marginBottom: '8px' }, children: "Flowchart Diagram" }), (0, jsx_runtime_1.jsx)("div", { style: { height: '350px' }, children: details.layout && (0, jsx_runtime_1.jsx)(WorkflowVisualizer_1.WorkflowVisualizer, { layout: details.layout }) })] }), (0, jsx_runtime_1.jsxs)("div", { children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", style: { display: 'block', marginBottom: '8px' }, children: "Diagnostics & Validation" }), diagnostics.length === 0 ? ((0, jsx_runtime_1.jsx)("div", { style: { padding: '12px', background: 'rgba(16, 185, 129, 0.08)', border: '1px solid rgba(16, 185, 129, 0.2)', borderRadius: '8px', color: '#34d399', fontSize: '13px' }, children: "\u2713 Definition is valid and clean. No warnings detected." })) : ((0, jsx_runtime_1.jsx)("div", { style: { display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '180px', overflowY: 'auto' }, children: diagnostics.map((diag, idx) => {
                            const severityName = getSeverityName(diag.severity);
                            return ((0, jsx_runtime_1.jsxs)("div", { style: {
                                    padding: '10px 14px',
                                    background: 'rgba(30, 41, 59, 0.5)',
                                    border: '1px solid #334155',
                                    borderRadius: '8px',
                                    fontSize: '12px',
                                    display: 'flex',
                                    flexDirection: 'column',
                                    gap: '4px'
                                }, children: [(0, jsx_runtime_1.jsxs)("div", { style: { display: 'flex', justifyContent: 'space-between', alignItems: 'center' }, children: [(0, jsx_runtime_1.jsxs)("span", { style: { fontWeight: '700', color: '#f3f4f6' }, children: [diag.code, " ", diag.nodeName && `[Node: ${diag.nodeName}]`] }), (0, jsx_runtime_1.jsx)("span", { className: `arora-badge ${getSeverityBadgeClass(severityName)}`, style: { fontSize: '10px', padding: '2px 6px' }, children: severityName })] }), (0, jsx_runtime_1.jsx)("div", { style: { color: '#cbd5e1' }, children: diag.message }), diag.suggestion && ((0, jsx_runtime_1.jsxs)("div", { style: { color: '#a855f7', fontStyle: 'italic', marginTop: '2px' }, children: ["\uD83D\uDCA1 Suggestion: ", diag.suggestion] }))] }, `diag-${idx}`));
                        }) }))] }), (0, jsx_runtime_1.jsxs)("div", { children: [(0, jsx_runtime_1.jsxs)("div", { style: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }, children: [(0, jsx_runtime_1.jsx)("span", { className: "arora-detail-label", children: "Mermaid Exporter" }), (0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-secondary", onClick: handleCopyMermaid, style: { padding: '4px 8px', fontSize: '11px' }, children: copied ? 'Copied! ✓' : 'Copy Code' })] }), (0, jsx_runtime_1.jsx)("pre", { className: "arora-code-panel", style: { maxHeight: '150px', overflowY: 'auto', fontSize: '11px', margin: 0 }, children: details.mermaid || '' })] })] }));
};
exports.DefinitionDetailsView = DefinitionDetailsView;
