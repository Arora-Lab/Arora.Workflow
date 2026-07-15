import React from 'react';
import { client as defaultClient } from '@arora/workflow-client';
export interface AroraWorkflowContextType {
    client: typeof defaultClient;
    currentUser: string;
}
export interface AroraWorkflowProviderProps {
    baseUrl?: string;
    client?: typeof defaultClient;
    currentUser?: string;
    children: React.ReactNode;
}
export declare const AroraWorkflowProvider: React.FC<AroraWorkflowProviderProps>;
export declare const useAroraWorkflowContext: () => AroraWorkflowContextType;
