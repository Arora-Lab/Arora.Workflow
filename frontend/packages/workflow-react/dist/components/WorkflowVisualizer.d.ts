import React from 'react';
import type { WorkflowLayout } from '@arora/workflow-client';
interface WorkflowVisualizerProps {
    layout: WorkflowLayout;
    activeNodeName?: string | null;
    completedNodeNames?: string[];
    failedNodeNames?: string[];
    nodeStates?: Record<string, 'active' | 'completed' | 'future' | 'bypassed' | 'cancelled' | 'failed'>;
    completedConnectionIds?: string[];
}
export declare const WorkflowVisualizer: React.FC<WorkflowVisualizerProps>;
export {};
