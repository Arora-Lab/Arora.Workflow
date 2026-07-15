import { useState, useEffect, useCallback } from 'react';
import { listWorkflowInstances } from '@arora/workflow-client';
import { useAroraWorkflowContext } from '../context/AroraWorkflowContext';
import type { WorkflowInstanceSummary } from '@arora/workflow-client';

export const useWorkflowInstances = (
  initialPage = 1,
  initialPageSize = 10,
  initialDefinitionId?: string,
  initialStatus?: string
) => {
  const { client } = useAroraWorkflowContext();
  const [instances, setInstances] = useState<WorkflowInstanceSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [page, setPage] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [definitionId, setDefinitionId] = useState<string | undefined>(initialDefinitionId);
  const [status, setStatus] = useState<string | undefined>(initialStatus);

  const fetchInstances = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const queryParams: Record<string, any> = { Page: page, PageSize: pageSize };
      if (definitionId) queryParams.DefinitionId = definitionId;
      if (status) queryParams.Status = status;

      const { data, error: apiError } = await listWorkflowInstances({
        client,
        query: queryParams,
      });

      if (apiError) {
        throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch instances');
      }

      if (data) {
        setInstances(data.items || []);
        setTotalCount(data.totalCount || 0);
      }
    } catch (err: any) {
      setError(err instanceof Error ? err : new Error(String(err)));
    } finally {
      setLoading(false);
    }
  }, [client, page, pageSize, definitionId, status]);

  useEffect(() => {
    fetchInstances();
  }, [fetchInstances]);

  return {
    instances,
    totalCount,
    loading,
    error,
    refetch: fetchInstances,
    page,
    setPage,
    pageSize,
    setPageSize,
    definitionId,
    setDefinitionId,
    status,
    setStatus,
  };
};
