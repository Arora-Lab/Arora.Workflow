export interface HistoryItem {
    id: string;
    instanceId: string;
    stepName?: string | null;
    action: string;
    timestamp: string;
    actor?: string | null;
    sequence: number;
    nodeId?: string | null;
    fromState?: string | null;
    toState?: string | null;
    comment?: string | null;
}
export type NodeVisualStatus = 'active' | 'completed' | 'future' | 'bypassed' | 'cancelled' | 'failed';
export interface PlaybackSnapshot {
    activeNodeIds: string[];
    completedNodeIds: string[];
    failedNodeIds: string[];
    cancelledNodeIds: string[];
    bypassedNodeIds: string[];
    futureNodeIds: string[];
    completedConnectionIds: string[];
    nodeStates: Record<string, NodeVisualStatus>;
}
export interface WorkflowNode {
    name: string;
}
export interface WorkflowLayout {
    nodes?: WorkflowNode[] | null;
}
export declare function derivePlaybackSnapshot(history: HistoryItem[], targetSequence: number, layout: WorkflowLayout): PlaybackSnapshot;
