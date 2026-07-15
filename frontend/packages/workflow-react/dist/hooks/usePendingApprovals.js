"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.usePendingApprovals = void 0;
const react_1 = require("react");
const workflow_client_1 = require("@arora/workflow-client");
const AroraWorkflowContext_1 = require("../context/AroraWorkflowContext");
const usePendingApprovals = () => {
    const { client, currentUser } = (0, AroraWorkflowContext_1.useAroraWorkflowContext)();
    const [approvals, setApprovals] = (0, react_1.useState)([]);
    const [loading, setLoading] = (0, react_1.useState)(true);
    const [error, setError] = (0, react_1.useState)(null);
    const fetchApprovals = (0, react_1.useCallback)(async () => {
        if (!currentUser)
            return;
        setLoading(true);
        setError(null);
        try {
            const { data, error: apiError } = await (0, workflow_client_1.getApiApprovalsPendingByUser)({
                client,
                path: { user: currentUser },
            });
            if (apiError) {
                throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch pending approvals');
            }
            if (data) {
                // Cast the unknown data to PendingApproval[]
                setApprovals(data);
            }
        }
        catch (err) {
            setError(err instanceof Error ? err : new Error(String(err)));
            setApprovals([]);
        }
        finally {
            setLoading(false);
        }
    }, [client, currentUser]);
    const approve = (0, react_1.useCallback)(async (approvalId) => {
        try {
            const { error: apiError } = await (0, workflow_client_1.postApiApprovalsByIdApprove)({
                client,
                path: { id: approvalId },
            });
            if (apiError) {
                throw new Error(typeof apiError === 'string' ? apiError : 'Failed to approve');
            }
            // Refresh list
            await fetchApprovals();
        }
        catch (err) {
            throw err instanceof Error ? err : new Error(String(err));
        }
    }, [client, fetchApprovals]);
    const reject = (0, react_1.useCallback)(async (approvalId) => {
        try {
            const { error: apiError } = await (0, workflow_client_1.postApiApprovalsByIdReject)({
                client,
                path: { id: approvalId },
            });
            if (apiError) {
                throw new Error(typeof apiError === 'string' ? apiError : 'Failed to reject');
            }
            // Refresh list
            await fetchApprovals();
        }
        catch (err) {
            throw err instanceof Error ? err : new Error(String(err));
        }
    }, [client, fetchApprovals]);
    (0, react_1.useEffect)(() => {
        fetchApprovals();
    }, [fetchApprovals]);
    return {
        approvals,
        loading,
        error,
        refetch: fetchApprovals,
        approve,
        reject,
    };
};
exports.usePendingApprovals = usePendingApprovals;
