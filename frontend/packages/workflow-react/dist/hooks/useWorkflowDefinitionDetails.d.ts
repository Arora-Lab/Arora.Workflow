import type { WorkflowDefinitionDetails } from '@arora/workflow-client';
export declare const useWorkflowDefinitionDetails: (id: string | null) => {
    details: WorkflowDefinitionDetails | null;
    loading: boolean;
    error: Error | null;
    refetch: () => Promise<void>;
};
