"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.useAroraWorkflowContext = exports.AroraWorkflowProvider = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
const workflow_client_1 = require("@arora/workflow-client");
const AroraWorkflowContext = (0, react_1.createContext)(undefined);
const AroraWorkflowProvider = ({ baseUrl, client, currentUser = 'tester', children, }) => {
    const activeClient = (0, react_1.useMemo)(() => {
        if (client)
            return client;
        if (baseUrl) {
            workflow_client_1.client.setConfig({ baseUrl });
            return workflow_client_1.client;
        }
        return workflow_client_1.client;
    }, [client, baseUrl]);
    const value = (0, react_1.useMemo)(() => ({
        client: activeClient,
        currentUser,
    }), [activeClient, currentUser]);
    return ((0, jsx_runtime_1.jsx)(AroraWorkflowContext.Provider, { value: value, children: children }));
};
exports.AroraWorkflowProvider = AroraWorkflowProvider;
const useAroraWorkflowContext = () => {
    const context = (0, react_1.useContext)(AroraWorkflowContext);
    if (!context) {
        throw new Error('useAroraWorkflowContext must be used within an AroraWorkflowProvider');
    }
    return context;
};
exports.useAroraWorkflowContext = useAroraWorkflowContext;
