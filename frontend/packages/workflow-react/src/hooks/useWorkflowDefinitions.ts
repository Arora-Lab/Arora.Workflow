import { useState, useEffect, useCallback } from 'react';
import { listWorkflowDefinitions } from '@arora/workflow-client';
import { useAroraWorkflowContext } from '../context/AroraWorkflowContext';
import type { WorkflowDefinitionSummary } from '@arora/workflow-client';

export const useWorkflowDefinitions = (initialPage = 1, initialPageSize = 10) => {
  const { client } = useAroraWorkflowContext();
  const [definitions, setDefinitions] = useState<WorkflowDefinitionSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [page, setPage] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);

  const fetchDefinitions = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const { data, error: apiError } = await listWorkflowDefinitions({
        client,
        query: { Page: page, PageSize: pageSize },
      });

      if (apiError) {
        throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch definitions');
      }

      if (data) {
        setDefinitions(data.items || []);
        setTotalCount(data.totalCount || 0);
      }
    } catch (err: any) {
      setError(err instanceof Error ? err : new Error(String(err)));
    } finally {
      setLoading(false);
    }
  }, [client, page, pageSize]);

  useEffect(() => {
    fetchDefinitions();
  }, [fetchDefinitions]);

  return {
    definitions,
    totalCount,
    loading,
    error,
    refetch: fetchDefinitions,
    page,
    setPage,
    pageSize,
    setPageSize,
  };
};
