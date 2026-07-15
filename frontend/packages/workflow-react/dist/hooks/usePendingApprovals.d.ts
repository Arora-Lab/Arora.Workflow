export interface ActorInfo {
    id: string;
    displayName: string;
}
export interface PendingApproval {
    approvalId: string;
    workflowInstanceId: string;
    workflowName: string;
    correlationId: string;
    stepName: string;
    assignedActor: ActorInfo;
    createdAt: string;
    deadlineAt?: string | null;
}
export declare const usePendingApprovals: () => {
    approvals: PendingApproval[];
    loading: boolean;
    error: Error | null;
    refetch: () => Promise<void>;
    approve: (approvalId: string) => Promise<void>;
    reject: (approvalId: string) => Promise<void>;
};
