"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.useWorkflowInstanceHistory = void 0;
const react_1 = require("react");
const workflow_client_1 = require("@arora/workflow-client");
const AroraWorkflowContext_1 = require("../context/AroraWorkflowContext");
const useWorkflowInstanceHistory = (instanceId, initialPage = 1, initialPageSize = 50) => {
    const { client } = (0, AroraWorkflowContext_1.useAroraWorkflowContext)();
    const [history, setHistory] = (0, react_1.useState)([]);
    const [totalCount, setTotalCount] = (0, react_1.useState)(0);
    const [loading, setLoading] = (0, react_1.useState)(true);
    const [error, setError] = (0, react_1.useState)(null);
    const [page, setPage] = (0, react_1.useState)(initialPage);
    const [pageSize, setPageSize] = (0, react_1.useState)(initialPageSize);
    const fetchHistory = (0, react_1.useCallback)(async () => {
        if (!instanceId)
            return;
        setLoading(true);
        setError(null);
        try {
            const { data, error: apiError } = await (0, workflow_client_1.getWorkflowInstanceHistory)({
                client,
                path: { id: instanceId },
                query: { page, pageSize },
            });
            if (apiError) {
                throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch history');
            }
            if (data) {
                setHistory(data.items || []);
                setTotalCount(data.totalCount || 0);
            }
        }
        catch (err) {
            setError(err instanceof Error ? err : new Error(String(err)));
            setHistory([]);
        }
        finally {
            setLoading(false);
        }
    }, [client, instanceId, page, pageSize]);
    (0, react_1.useEffect)(() => {
        fetchHistory();
    }, [fetchHistory]);
    return {
        history,
        totalCount,
        loading,
        error,
        refetch: fetchHistory,
        page,
        setPage,
        pageSize,
        setPageSize,
    };
};
exports.useWorkflowInstanceHistory = useWorkflowInstanceHistory;
