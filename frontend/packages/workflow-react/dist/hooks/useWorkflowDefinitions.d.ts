import type { WorkflowDefinitionSummary } from '@arora/workflow-client';
export declare const useWorkflowDefinitions: (initialPage?: number, initialPageSize?: number) => {
    definitions: WorkflowDefinitionSummary[];
    totalCount: number;
    loading: boolean;
    error: Error | null;
    refetch: () => Promise<void>;
    page: number;
    setPage: import("react").Dispatch<import("react").SetStateAction<number>>;
    pageSize: number;
    setPageSize: import("react").Dispatch<import("react").SetStateAction<number>>;
};
