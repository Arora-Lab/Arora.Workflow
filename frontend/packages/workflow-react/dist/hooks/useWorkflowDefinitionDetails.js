"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.useWorkflowDefinitionDetails = void 0;
const react_1 = require("react");
const workflow_client_1 = require("@arora/workflow-client");
const AroraWorkflowContext_1 = require("../context/AroraWorkflowContext");
const useWorkflowDefinitionDetails = (id) => {
    const { client } = (0, AroraWorkflowContext_1.useAroraWorkflowContext)();
    const [details, setDetails] = (0, react_1.useState)(null);
    const [loading, setLoading] = (0, react_1.useState)(false);
    const [error, setError] = (0, react_1.useState)(null);
    const fetchDetails = (0, react_1.useCallback)(async () => {
        if (!id) {
            setDetails(null);
            return;
        }
        setLoading(true);
        setError(null);
        try {
            const { data, error: apiError } = await (0, workflow_client_1.getWorkflowDefinitionDetails)({
                client,
                path: { id },
            });
            if (apiError) {
                throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch definition details');
            }
            if (data) {
                setDetails(data);
            }
        }
        catch (err) {
            setError(err instanceof Error ? err : new Error(String(err)));
        }
        finally {
            setLoading(false);
        }
    }, [client, id]);
    (0, react_1.useEffect)(() => {
        fetchDetails();
    }, [fetchDetails]);
    return {
        details,
        loading,
        error,
        refetch: fetchDetails,
    };
};
exports.useWorkflowDefinitionDetails = useWorkflowDefinitionDetails;
