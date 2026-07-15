"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DefinitionList = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const useWorkflowDefinitions_1 = require("../hooks/useWorkflowDefinitions");
const DefinitionList = ({ onSelectDefinition, selectedDefinitionId, }) => {
    const { definitions, totalCount, loading, error, page, setPage, pageSize } = (0, useWorkflowDefinitions_1.useWorkflowDefinitions)();
    if (loading && definitions.length === 0) {
        return (0, jsx_runtime_1.jsx)("div", { className: "arora-loading-spinner" });
    }
    if (error) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsxs)("div", { className: "arora-empty-state", style: { borderColor: 'var(--arora-color-rejected)' }, children: [(0, jsx_runtime_1.jsx)("p", { style: { color: 'var(--arora-color-rejected)', fontWeight: 'bold' }, children: "Error loading definitions" }), (0, jsx_runtime_1.jsx)("p", { style: { fontSize: '13px' }, children: error.message })] }) }));
    }
    if (definitions.length === 0) {
        return ((0, jsx_runtime_1.jsx)("div", { className: "arora-card", children: (0, jsx_runtime_1.jsx)("div", { className: "arora-empty-state", children: (0, jsx_runtime_1.jsx)("p", { children: "No workflow definitions found." }) }) }));
    }
    const totalPages = Math.ceil(totalCount / pageSize);
    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-card", children: [(0, jsx_runtime_1.jsxs)("h3", { className: "arora-card-title", children: [(0, jsx_runtime_1.jsx)("span", { children: "Workflow Definitions" }), (0, jsx_runtime_1.jsxs)("span", { style: { fontSize: '12px', fontWeight: 'normal', color: 'var(--arora-text-muted)' }, children: ["Total: ", totalCount] })] }), (0, jsx_runtime_1.jsx)("div", { className: "arora-table-wrapper", children: (0, jsx_runtime_1.jsxs)("table", { className: "arora-table", children: [(0, jsx_runtime_1.jsx)("thead", { children: (0, jsx_runtime_1.jsxs)("tr", { children: [(0, jsx_runtime_1.jsx)("th", { children: "Name" }), (0, jsx_runtime_1.jsx)("th", { children: "Version" }), (0, jsx_runtime_1.jsx)("th", { children: "Steps" }), (0, jsx_runtime_1.jsx)("th", { children: "Created At" })] }) }), (0, jsx_runtime_1.jsx)("tbody", { children: definitions.map((def) => {
                                const defId = def.id || '';
                                if (!defId)
                                    return null;
                                const isActive = defId === selectedDefinitionId;
                                return ((0, jsx_runtime_1.jsxs)("tr", { onClick: () => onSelectDefinition?.(defId), style: isActive ? { backgroundColor: 'var(--arora-primary-light)' } : undefined, children: [(0, jsx_runtime_1.jsx)("td", { style: { fontWeight: '600' }, children: def.name || 'Unnamed' }), (0, jsx_runtime_1.jsxs)("td", { children: ["v", def.version] }), (0, jsx_runtime_1.jsx)("td", { children: def.stepCount }), (0, jsx_runtime_1.jsx)("td", { children: def.createdAt ? new Date(def.createdAt).toLocaleDateString() : '' })] }, defId));
                            }) })] }) }), totalPages > 1 && ((0, jsx_runtime_1.jsxs)("div", { className: "arora-pagination", children: [(0, jsx_runtime_1.jsxs)("span", { className: "arora-pagination-info", children: ["Page ", page, " of ", totalPages] }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-pagination-controls", children: [(0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-secondary", disabled: page <= 1, onClick: () => setPage(page - 1), style: { padding: '6px 12px', fontSize: '12px' }, children: "Previous" }), (0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-secondary", disabled: page >= totalPages, onClick: () => setPage(page + 1), style: { padding: '6px 12px', fontSize: '12px' }, children: "Next" })] })] }))] }));
};
exports.DefinitionList = DefinitionList;
