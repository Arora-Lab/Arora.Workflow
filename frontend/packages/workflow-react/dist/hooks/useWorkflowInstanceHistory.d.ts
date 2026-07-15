import type { WorkflowHistoryItem } from '@arora/workflow-client';
export declare const useWorkflowInstanceHistory: (instanceId: string, initialPage?: number, initialPageSize?: number) => {
    history: WorkflowHistoryItem[];
    totalCount: number;
    loading: boolean;
    error: Error | null;
    refetch: () => Promise<void>;
    page: number;
    setPage: import("react").Dispatch<import("react").SetStateAction<number>>;
    pageSize: number;
    setPageSize: import("react").Dispatch<import("react").SetStateAction<number>>;
};
