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

export function derivePlaybackSnapshot(
  history: HistoryItem[],
  targetSequence: number,
  layout: WorkflowLayout
): PlaybackSnapshot {
  const activeNodeIds = new Set<string>();
  const completedNodeIds = new Set<string>();
  const failedNodeIds = new Set<string>();
  const cancelledNodeIds = new Set<string>();
  const completedConnectionIds = new Set<string>();

  // Filter and sort history items up to and including targetSequence
  const filteredHistory = history
    .filter((h) => h.sequence <= targetSequence)
    .sort((a, b) => a.sequence - b.sequence);

  let isTerminal = false;

  for (const item of filteredHistory) {
    const actionLower = item.action.toLowerCase();

    // Check terminal events or states
    if (
      actionLower.includes('completed') ||
      item.toState?.toLowerCase() === 'completed'
    ) {
      isTerminal = true;
    } else if (
      actionLower.includes('rejected') ||
      item.toState?.toLowerCase() === 'rejected'
    ) {
      isTerminal = true;
    } else if (
      actionLower.includes('cancelled') ||
      item.toState?.toLowerCase() === 'cancelled'
    ) {
      isTerminal = true;
    }

    if (actionLower.includes('started')) {
      const initialNode = item.nodeId || item.toState || 'Start';
      activeNodeIds.add(initialNode);
    } else if (actionLower.includes('transitioned')) {
      const realFromNode = item.fromState || '';
      const realToNode = item.toState || '';

      if (realFromNode) {
        completedNodeIds.add(realFromNode);
        activeNodeIds.delete(realFromNode);
      }

      if (realToNode) {
        if (realToNode.toLowerCase() === 'completed') {
          completedNodeIds.add(realToNode);
        } else if (realToNode.toLowerCase() === 'rejected') {
          failedNodeIds.add(realToNode);
        } else if (realToNode.toLowerCase() === 'cancelled') {
          cancelledNodeIds.add(realToNode);
        } else {
          activeNodeIds.add(realToNode);
        }
      }

      if (realFromNode && realToNode) {
        completedConnectionIds.add(`${realFromNode.toLowerCase()}->${realToNode.toLowerCase()}`);
      }
    } else if (actionLower.includes('cancelled')) {
      const lastActive = item.fromState || item.nodeId || '';
      if (lastActive) {
        activeNodeIds.delete(lastActive);
        cancelledNodeIds.add(lastActive);
      }
      cancelledNodeIds.add('Cancelled');
    } else if (actionLower.includes('rejected')) {
      const rejectedNode = item.nodeId || item.stepName || '';
      if (rejectedNode) {
        activeNodeIds.delete(rejectedNode);
        failedNodeIds.add(rejectedNode);
      }
      failedNodeIds.add('Rejected');
    }
  }

  const allNodeNames = (layout?.nodes || []).map((n) => n.name).filter(Boolean);
  const bypassedNodeIds = new Set<string>();
  const futureNodeIds = new Set<string>();

  for (const nodeName of allNodeNames) {
    if (
      !activeNodeIds.has(nodeName) &&
      !completedNodeIds.has(nodeName) &&
      !failedNodeIds.has(nodeName) &&
      !cancelledNodeIds.has(nodeName)
    ) {
      if (isTerminal) {
        bypassedNodeIds.add(nodeName);
      } else {
        futureNodeIds.add(nodeName);
      }
    }
  }

  const nodeStates: Record<string, NodeVisualStatus> = {};
  allNodeNames.forEach((n) => {
    if (activeNodeIds.has(n)) nodeStates[n] = 'active';
    else if (completedNodeIds.has(n)) nodeStates[n] = 'completed';
    else if (failedNodeIds.has(n)) nodeStates[n] = 'failed';
    else if (cancelledNodeIds.has(n)) nodeStates[n] = 'cancelled';
    else if (bypassedNodeIds.has(n)) nodeStates[n] = 'bypassed';
    else nodeStates[n] = 'future';
  });

  return {
    activeNodeIds: Array.from(activeNodeIds),
    completedNodeIds: Array.from(completedNodeIds),
    failedNodeIds: Array.from(failedNodeIds),
    cancelledNodeIds: Array.from(cancelledNodeIds),
    bypassedNodeIds: Array.from(bypassedNodeIds),
    futureNodeIds: Array.from(futureNodeIds),
    completedConnectionIds: Array.from(completedConnectionIds),
    nodeStates,
  };
}
