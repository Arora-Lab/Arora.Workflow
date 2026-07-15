import { useState, useEffect, useCallback } from 'react';
import { getApiApprovalsPendingByUser, postApiApprovalsByIdApprove, postApiApprovalsByIdReject } from '@arora/workflow-client';
import { useAroraWorkflowContext } from '../context/AroraWorkflowContext';

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

export const usePendingApprovals = () => {
  const { client, currentUser } = useAroraWorkflowContext();
  const [approvals, setApprovals] = useState<PendingApproval[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchApprovals = useCallback(async () => {
    if (!currentUser) return;
    setLoading(true);
    setError(null);
    try {
      const { data, error: apiError } = await getApiApprovalsPendingByUser({
        client,
        path: { user: currentUser },
      });

      if (apiError) {
        throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch pending approvals');
      }

      if (data) {
        // Cast the unknown data to PendingApproval[]
        setApprovals(data as any as PendingApproval[]);
      }
    } catch (err: any) {
      setError(err instanceof Error ? err : new Error(String(err)));
      setApprovals([]);
    } finally {
      setLoading(false);
    }
  }, [client, currentUser]);

  const approve = useCallback(async (approvalId: string) => {
    try {
      const { error: apiError } = await postApiApprovalsByIdApprove({
        client,
        path: { id: approvalId },
      });

      if (apiError) {
        throw new Error(typeof apiError === 'string' ? apiError : 'Failed to approve');
      }

      // Refresh list
      await fetchApprovals();
    } catch (err: any) {
      throw err instanceof Error ? err : new Error(String(err));
    }
  }, [client, fetchApprovals]);

  const reject = useCallback(async (approvalId: string) => {
    try {
      const { error: apiError } = await postApiApprovalsByIdReject({
        client,
        path: { id: approvalId },
      });

      if (apiError) {
        throw new Error(typeof apiError === 'string' ? apiError : 'Failed to reject');
      }

      // Refresh list
      await fetchApprovals();
    } catch (err: any) {
      throw err instanceof Error ? err : new Error(String(err));
    }
  }, [client, fetchApprovals]);

  useEffect(() => {
    fetchApprovals();
  }, [fetchApprovals]);

  return {
    approvals,
    loading,
    error,
    refetch: fetchApprovals,
    approve,
    reject,
  };
};
