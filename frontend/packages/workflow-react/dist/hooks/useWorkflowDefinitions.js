"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.useWorkflowDefinitions = void 0;
const react_1 = require("react");
const workflow_client_1 = require("@arora/workflow-client");
const AroraWorkflowContext_1 = require("../context/AroraWorkflowContext");
const useWorkflowDefinitions = (initialPage = 1, initialPageSize = 10) => {
    const { client } = (0, AroraWorkflowContext_1.useAroraWorkflowContext)();
    const [definitions, setDefinitions] = (0, react_1.useState)([]);
    const [totalCount, setTotalCount] = (0, react_1.useState)(0);
    const [loading, setLoading] = (0, react_1.useState)(true);
    const [error, setError] = (0, react_1.useState)(null);
    const [page, setPage] = (0, react_1.useState)(initialPage);
    const [pageSize, setPageSize] = (0, react_1.useState)(initialPageSize);
    const fetchDefinitions = (0, react_1.useCallback)(async () => {
        setLoading(true);
        setError(null);
        try {
            const { data, error: apiError } = await (0, workflow_client_1.listWorkflowDefinitions)({
                client,
                query: { Page: page, PageSize: pageSize },
            });
            if (apiError) {
                throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch definitions');
            }
            if (data) {
                setDefinitions(data.items || []);
                setTotalCount(data.totalCount || 0);
            }
        }
        catch (err) {
            setError(err instanceof Error ? err : new Error(String(err)));
        }
        finally {
            setLoading(false);
        }
    }, [client, page, pageSize]);
    (0, react_1.useEffect)(() => {
        fetchDefinitions();
    }, [fetchDefinitions]);
    return {
        definitions,
        totalCount,
        loading,
        error,
        refetch: fetchDefinitions,
        page,
        setPage,
        pageSize,
        setPageSize,
    };
};
exports.useWorkflowDefinitions = useWorkflowDefinitions;
