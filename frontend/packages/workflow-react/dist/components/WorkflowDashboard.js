"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.WorkflowDashboard = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
const DefinitionList_1 = require("./DefinitionList");
const InstanceList_1 = require("./InstanceList");
const InstanceDetailsView_1 = require("./InstanceDetailsView");
const HistoryTimeline_1 = require("./HistoryTimeline");
const PendingApprovalsList_1 = require("./PendingApprovalsList");
const AroraWorkflowContext_1 = require("../context/AroraWorkflowContext");
const WorkflowDashboard = () => {
    const { currentUser } = (0, AroraWorkflowContext_1.useAroraWorkflowContext)();
    const [activeTab, setActiveTab] = (0, react_1.useState)('instances');
    const [selectedInstanceId, setSelectedInstanceId] = (0, react_1.useState)(null);
    const [selectedDefinitionId, setSelectedDefinitionId] = (0, react_1.useState)(null);
    const [isLightTheme, setIsLightTheme] = (0, react_1.useState)(false);
    const handleSelectInstance = (id) => {
        setSelectedInstanceId(id);
    };
    const handleSelectDefinition = (id) => {
        setSelectedDefinitionId(id);
        setActiveTab('instances');
    };
    const toggleTheme = () => {
        setIsLightTheme((prev) => !prev);
    };
    return ((0, jsx_runtime_1.jsxs)("div", { className: `arora-dashboard-container ${isLightTheme ? 'arora-light-theme' : ''}`, children: [(0, jsx_runtime_1.jsxs)("header", { className: "arora-header", children: [(0, jsx_runtime_1.jsxs)("div", { className: "arora-title-group", children: [(0, jsx_runtime_1.jsx)("h1", { children: "Arora Workflow" }), (0, jsx_runtime_1.jsx)("p", { children: "Enterprise Approval Engine Management Dashboard" })] }), (0, jsx_runtime_1.jsxs)("div", { style: { display: 'flex', alignItems: 'center', gap: '16px' }, children: [(0, jsx_runtime_1.jsx)("button", { className: "arora-btn arora-btn-secondary", onClick: toggleTheme, style: { padding: '6px 12px', fontSize: '13px' }, children: isLightTheme ? 'Dark Mode 🌙' : 'Light Mode ☀️' }), (0, jsx_runtime_1.jsxs)("div", { className: "arora-user-badge", children: [(0, jsx_runtime_1.jsx)("div", { className: "avatar", children: currentUser.substring(0, 2).toUpperCase() }), (0, jsx_runtime_1.jsxs)("span", { children: ["Logged in as: ", (0, jsx_runtime_1.jsx)("strong", { children: currentUser })] })] })] })] }), (0, jsx_runtime_1.jsx)("div", { style: { display: 'flex', justifyContent: 'space-between', alignItems: 'center' }, children: (0, jsx_runtime_1.jsxs)("div", { className: "arora-tabs", children: [(0, jsx_runtime_1.jsx)("button", { className: `arora-tab-button ${activeTab === 'instances' ? 'active' : ''}`, onClick: () => setActiveTab('instances'), children: "Instances" }), (0, jsx_runtime_1.jsx)("button", { className: `arora-tab-button ${activeTab === 'definitions' ? 'active' : ''}`, onClick: () => setActiveTab('definitions'), children: "Definitions" }), (0, jsx_runtime_1.jsx)("button", { className: `arora-tab-button ${activeTab === 'approvals' ? 'active' : ''}`, onClick: () => setActiveTab('approvals'), children: "My Approvals" })] }) }), (0, jsx_runtime_1.jsxs)("main", { style: { minHeight: '500px' }, children: [activeTab === 'instances' && ((0, jsx_runtime_1.jsxs)("div", { className: "arora-layout-grid", children: [(0, jsx_runtime_1.jsx)(InstanceList_1.InstanceList, { onSelectInstance: handleSelectInstance, selectedInstanceId: selectedInstanceId || undefined, definitionIdFilter: selectedDefinitionId || undefined }), (0, jsx_runtime_1.jsxs)("div", { style: { display: 'flex', flexDirection: 'column', gap: '24px' }, children: [(0, jsx_runtime_1.jsx)(InstanceDetailsView_1.InstanceDetailsView, { instanceId: selectedInstanceId || '' }), selectedInstanceId && (0, jsx_runtime_1.jsx)(HistoryTimeline_1.HistoryTimeline, { instanceId: selectedInstanceId })] })] })), activeTab === 'definitions' && ((0, jsx_runtime_1.jsx)(DefinitionList_1.DefinitionList, { onSelectDefinition: handleSelectDefinition, selectedDefinitionId: selectedDefinitionId || undefined })), activeTab === 'approvals' && ((0, jsx_runtime_1.jsx)(PendingApprovalsList_1.PendingApprovalsList, { onSelectInstance: (id) => {
                            setSelectedInstanceId(id);
                            setActiveTab('instances');
                        } }))] })] }));
};
exports.WorkflowDashboard = WorkflowDashboard;
