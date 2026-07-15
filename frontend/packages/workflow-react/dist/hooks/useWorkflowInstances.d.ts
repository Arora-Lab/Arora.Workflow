import type { WorkflowInstanceSummary } from '@arora/workflow-client';
export declare const useWorkflowInstances: (initialPage?: number, initialPageSize?: number, initialDefinitionId?: string, initialStatus?: string) => {
    instances: WorkflowInstanceSummary[];
    totalCount: number;
    loading: boolean;
    error: Error | null;
    refetch: () => Promise<void>;
    page: number;
    setPage: import("react").Dispatch<import("react").SetStateAction<number>>;
    pageSize: number;
    setPageSize: import("react").Dispatch<import("react").SetStateAction<number>>;
    definitionId: string | undefined;
    setDefinitionId: import("react").Dispatch<import("react").SetStateAction<string | undefined>>;
    status: string | undefined;
    setStatus: import("react").Dispatch<import("react").SetStateAction<string | undefined>>;
};
