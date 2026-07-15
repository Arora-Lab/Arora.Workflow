import React, { createContext, useContext, useMemo } from 'react';
import { client as defaultClient } from '@arora/workflow-client';

export interface AroraWorkflowContextType {
  client: typeof defaultClient;
  currentUser: string;
}

const AroraWorkflowContext = createContext<AroraWorkflowContextType | undefined>(undefined);

export interface AroraWorkflowProviderProps {
  baseUrl?: string;
  client?: typeof defaultClient;
  currentUser?: string;
  children: React.ReactNode;
}

export const AroraWorkflowProvider: React.FC<AroraWorkflowProviderProps> = ({
  baseUrl,
  client,
  currentUser = 'tester',
  children,
}) => {
  const activeClient = useMemo(() => {
    if (client) return client;
    if (baseUrl) {
      defaultClient.setConfig({ baseUrl });
      return defaultClient;
    }
    return defaultClient;
  }, [client, baseUrl]);

  const value = useMemo(
    () => ({
      client: activeClient,
      currentUser,
    }),
    [activeClient, currentUser]
  );

  return (
    <AroraWorkflowContext.Provider value={value}>
      {children}
    </AroraWorkflowContext.Provider>
  );
};

export const useAroraWorkflowContext = () => {
  const context = useContext(AroraWorkflowContext);
  if (!context) {
    throw new Error('useAroraWorkflowContext must be used within an AroraWorkflowProvider');
  }
  return context;
};
