"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.useWorkflowInstances = void 0;
const react_1 = require("react");
const workflow_client_1 = require("@arora/workflow-client");
const AroraWorkflowContext_1 = require("../context/AroraWorkflowContext");
const useWorkflowInstances = (initialPage = 1, initialPageSize = 10, initialDefinitionId, initialStatus) => {
    const { client } = (0, AroraWorkflowContext_1.useAroraWorkflowContext)();
    const [instances, setInstances] = (0, react_1.useState)([]);
    const [totalCount, setTotalCount] = (0, react_1.useState)(0);
    const [loading, setLoading] = (0, react_1.useState)(true);
    const [error, setError] = (0, react_1.useState)(null);
    const [page, setPage] = (0, react_1.useState)(initialPage);
    const [pageSize, setPageSize] = (0, react_1.useState)(initialPageSize);
    const [definitionId, setDefinitionId] = (0, react_1.useState)(initialDefinitionId);
    const [status, setStatus] = (0, react_1.useState)(initialStatus);
    const fetchInstances = (0, react_1.useCallback)(async () => {
        setLoading(true);
        setError(null);
        try {
            const queryParams = { Page: page, PageSize: pageSize };
            if (definitionId)
                queryParams.DefinitionId = definitionId;
            if (status)
                queryParams.Status = status;
            const { data, error: apiError } = await (0, workflow_client_1.listWorkflowInstances)({
                client,
                query: queryParams,
            });
            if (apiError) {
                throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch instances');
            }
            if (data) {
                setInstances(data.items || []);
                setTotalCount(data.totalCount || 0);
            }
        }
        catch (err) {
            setError(err instanceof Error ? err : new Error(String(err)));
        }
        finally {
            setLoading(false);
        }
    }, [client, page, pageSize, definitionId, status]);
    (0, react_1.useEffect)(() => {
        fetchInstances();
    }, [fetchInstances]);
    return {
        instances,
        totalCount,
        loading,
        error,
        refetch: fetchInstances,
        page,
        setPage,
        pageSize,
        setPageSize,
        definitionId,
        setDefinitionId,
        status,
        setStatus,
    };
};
exports.useWorkflowInstances = useWorkflowInstances;
