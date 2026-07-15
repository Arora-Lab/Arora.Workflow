import { useState, useEffect, useCallback } from 'react';
import { getWorkflowInstanceHistory } from '@arora/workflow-client';
import { useAroraWorkflowContext } from '../context/AroraWorkflowContext';
import type { WorkflowHistoryItem } from '@arora/workflow-client';

export const useWorkflowInstanceHistory = (instanceId: string, initialPage = 1, initialPageSize = 50) => {
  const { client } = useAroraWorkflowContext();
  const [history, setHistory] = useState<WorkflowHistoryItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [page, setPage] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);

  const fetchHistory = useCallback(async () => {
    if (!instanceId) return;
    setLoading(true);
    setError(null);
    try {
      const { data, error: apiError } = await getWorkflowInstanceHistory({
        client,
        path: { id: instanceId },
        query: { page, pageSize },
      });

      if (apiError) {
        throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch history');
      }

      if (data) {
        setHistory(data.items || []);
        setTotalCount(data.totalCount || 0);
      }
    } catch (err: any) {
      setError(err instanceof Error ? err : new Error(String(err)));
      setHistory([]);
    } finally {
      setLoading(false);
    }
  }, [client, instanceId, page, pageSize]);

  useEffect(() => {
    fetchHistory();
  }, [fetchHistory]);

  return {
    history,
    totalCount,
    loading,
    error,
    refetch: fetchHistory,
    page,
    setPage,
    pageSize,
    setPageSize,
  };
};
