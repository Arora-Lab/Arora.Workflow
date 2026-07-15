import { useState, useEffect, useCallback } from 'react';
import { getWorkflowDefinitionDetails } from '@arora/workflow-client';
import { useAroraWorkflowContext } from '../context/AroraWorkflowContext';
import type { WorkflowDefinitionDetails } from '@arora/workflow-client';

export const useWorkflowDefinitionDetails = (id: string | null) => {
  const { client } = useAroraWorkflowContext();
  const [details, setDetails] = useState<WorkflowDefinitionDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const fetchDetails = useCallback(async () => {
    if (!id) {
      setDetails(null);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const { data, error: apiError } = await getWorkflowDefinitionDetails({
        client,
        path: { id },
      });

      if (apiError) {
        throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch definition details');
      }

      if (data) {
        setDetails(data);
      }
    } catch (err: any) {
      setError(err instanceof Error ? err : new Error(String(err)));
    } finally {
      setLoading(false);
    }
  }, [client, id]);

  useEffect(() => {
    fetchDetails();
  }, [fetchDetails]);

  return {
    details,
    loading,
    error,
    refetch: fetchDetails,
  };
};
