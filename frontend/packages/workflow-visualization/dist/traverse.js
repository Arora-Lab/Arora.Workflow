"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.derivePlaybackSnapshot = derivePlaybackSnapshot;
function derivePlaybackSnapshot(history, targetSequence, layout) {
    const activeNodeIds = new Set();
    const completedNodeIds = new Set();
    const failedNodeIds = new Set();
    const cancelledNodeIds = new Set();
    const completedConnectionIds = new Set();
    // Filter and sort history items up to and including targetSequence
    const filteredHistory = history
        .filter((h) => h.sequence <= targetSequence)
        .sort((a, b) => a.sequence - b.sequence);
    let isTerminal = false;
    for (const item of filteredHistory) {
        const actionLower = item.action.toLowerCase();
        // Check terminal events or states
        if (actionLower.includes('completed') ||
            item.toState?.toLowerCase() === 'completed') {
            isTerminal = true;
        }
        else if (actionLower.includes('rejected') ||
            item.toState?.toLowerCase() === 'rejected') {
            isTerminal = true;
        }
        else if (actionLower.includes('cancelled') ||
            item.toState?.toLowerCase() === 'cancelled') {
            isTerminal = true;
        }
        if (actionLower.includes('started')) {
            const initialNode = item.nodeId || item.toState || 'Start';
            activeNodeIds.add(initialNode);
        }
        else if (actionLower.includes('transitioned')) {
            const realFromNode = item.fromState || '';
            const realToNode = item.toState || '';
            if (realFromNode) {
                completedNodeIds.add(realFromNode);
                activeNodeIds.delete(realFromNode);
            }
            if (realToNode) {
                if (realToNode.toLowerCase() === 'completed') {
                    completedNodeIds.add(realToNode);
                }
                else if (realToNode.toLowerCase() === 'rejected') {
                    failedNodeIds.add(realToNode);
                }
                else if (realToNode.toLowerCase() === 'cancelled') {
                    cancelledNodeIds.add(realToNode);
                }
                else {
                    activeNodeIds.add(realToNode);
                }
            }
            if (realFromNode && realToNode) {
                completedConnectionIds.add(`${realFromNode.toLowerCase()}->${realToNode.toLowerCase()}`);
            }
        }
        else if (actionLower.includes('cancelled')) {
            const lastActive = item.fromState || item.nodeId || '';
            if (lastActive) {
                activeNodeIds.delete(lastActive);
                cancelledNodeIds.add(lastActive);
            }
            cancelledNodeIds.add('Cancelled');
        }
        else if (actionLower.includes('rejected')) {
            const rejectedNode = item.nodeId || item.stepName || '';
            if (rejectedNode) {
                activeNodeIds.delete(rejectedNode);
                failedNodeIds.add(rejectedNode);
            }
            failedNodeIds.add('Rejected');
        }
    }
    const allNodeNames = (layout?.nodes || []).map((n) => n.name).filter(Boolean);
    const bypassedNodeIds = new Set();
    const futureNodeIds = new Set();
    for (const nodeName of allNodeNames) {
        if (!activeNodeIds.has(nodeName) &&
            !completedNodeIds.has(nodeName) &&
            !failedNodeIds.has(nodeName) &&
            !cancelledNodeIds.has(nodeName)) {
            if (isTerminal) {
                bypassedNodeIds.add(nodeName);
            }
            else {
                futureNodeIds.add(nodeName);
            }
        }
    }
    const nodeStates = {};
    allNodeNames.forEach((n) => {
        if (activeNodeIds.has(n))
            nodeStates[n] = 'active';
        else if (completedNodeIds.has(n))
            nodeStates[n] = 'completed';
        else if (failedNodeIds.has(n))
            nodeStates[n] = 'failed';
        else if (cancelledNodeIds.has(n))
            nodeStates[n] = 'cancelled';
        else if (bypassedNodeIds.has(n))
            nodeStates[n] = 'bypassed';
        else
            nodeStates[n] = 'future';
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
