import { useState, useEffect, useCallback } from 'react';
import { getWorkflowInstance } from '@arora/workflow-client';
import { useAroraWorkflowContext } from '../context/AroraWorkflowContext';
import type { WorkflowInstanceDetails } from '@arora/workflow-client';

export const useWorkflowInstanceDetails = (instanceId: string) => {
  const { client } = useAroraWorkflowContext();
  const [instance, setInstance] = useState<WorkflowInstanceDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchDetails = useCallback(async () => {
    if (!instanceId) return;
    setLoading(true);
    setError(null);
    try {
      const { data, error: apiError } = await getWorkflowInstance({
        client,
        path: { id: instanceId },
      });

      if (apiError) {
        throw new Error(typeof apiError === 'string' ? apiError : 'Failed to fetch workflow instance details');
      }

      if (data) {
        setInstance(data);
      }
    } catch (err: any) {
      setError(err instanceof Error ? err : new Error(String(err)));
      setInstance(null);
    } finally {
      setLoading(false);
    }
  }, [client, instanceId]);

  useEffect(() => {
    fetchDetails();
  }, [fetchDetails]);

  return {
    instance,
    loading,
    error,
    refetch: fetchDetails,
  };
};
