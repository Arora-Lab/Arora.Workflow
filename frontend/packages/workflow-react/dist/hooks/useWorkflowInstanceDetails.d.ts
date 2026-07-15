import type { WorkflowInstanceDetails } from '@arora/workflow-client';
export declare const useWorkflowInstanceDetails: (instanceId: string) => {
    instance: WorkflowInstanceDetails | null;
    loading: boolean;
    error: Error | null;
    refetch: () => Promise<void>;
};
