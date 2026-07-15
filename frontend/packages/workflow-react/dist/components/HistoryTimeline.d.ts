import React from 'react';
import type { WorkflowHistoryItem } from '@arora/workflow-client';
export interface HistoryTimelineProps {
    instanceId: string;
    historyItems?: WorkflowHistoryItem[];
    selectedSequence?: number | null;
    onSelectSequence?: (seq: number) => void;
}
export declare const HistoryTimeline: React.FC<HistoryTimelineProps>;
