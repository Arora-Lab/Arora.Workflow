"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.useWorkflowInstanceDetails = void 0;
const react_1 = require("react");
const workflow_client_1 = require("@arora/workflow-client");
const AroraWorkflowContext_1 = require("../context/AroraWorkflowContext");
const useWorkflowInstanceDetails = (instanceId) => {
    const { client } = (0, AroraWorkflowContext_1.useAroraWorkflowContext)();
    const [instance, setInstance] = (0, react_1.useState)(null);
    const [loading, setLoading] = (0, react_1.useState)(true);
    const [error, setError] = (0, react_1.useState)(null);
    const fetchDetails = (0, react_1.useCallback)(async () => {
        if (!instanceId)
            return;
        setLoading(true);
        setError(null);
        try {
            const { data, error: apiError } = await (0, workflow_client_1.getWorkflowInstance)({
                client,
                path: { id: instanceId },
            });
            if (apiError) {
                throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch workflow instance details');
            }
            if (data) {
                setInstance(data);
            }
        }
        catch (err) {
            setError(err instanceof Error ? err : new Error(String(err)));
            setInstance(null);
        }
        finally {
            setLoading(false);
        }
    }, [client, instanceId]);
    (0, react_1.useEffect)(() => {
        fetchDetails();
    }, [fetchDetails]);
    return {
        instance,
        loading,
        error,
        refetch: fetchDetails,
    };
};
exports.useWorkflowInstanceDetails = useWorkflowInstanceDetails;
