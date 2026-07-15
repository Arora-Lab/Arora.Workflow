import React from 'react';
import type { WorkflowLayout } from '@arora/workflow-client';
interface WorkflowVisualizerProps {
    layout: WorkflowLayout;
    activeNodeName?: string | null;
    completedNodeNames?: string[];
    failedNodeNames?: string[];
}
export declare const WorkflowVisualizer: React.FC<WorkflowVisualizerProps>;
export {};
