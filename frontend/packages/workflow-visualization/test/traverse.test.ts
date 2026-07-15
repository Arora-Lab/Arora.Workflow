import { describe, it, expect } from 'vitest';
import { derivePlaybackSnapshot } from '../src/traverse.js';
import type { HistoryItem, WorkflowLayout } from '../src/traverse.js';

describe('derivePlaybackSnapshot', () => {
  const layout: WorkflowLayout = {
    nodes: [
      { name: 'Start' },
      { name: 'PendingManagerApproval' },
      { name: 'Completed' },
      { name: 'Rejected' },
      { name: 'Cancelled' }
    ]
  };

  it('calculates initial state sequence correctly', () => {
    const history: HistoryItem[] = [
      {
        id: '1',
        instanceId: 'inst-1',
        action: 'Started',
        timestamp: new Date().toISOString(),
        sequence: 1,
        toState: 'Start'
      }
    ];

    const snapshot = derivePlaybackSnapshot(history, 1, layout);
    expect(snapshot.activeNodeIds).toContain('Start');
    expect(snapshot.nodeStates['Start']).toBe('active');
    expect(snapshot.nodeStates['PendingManagerApproval']).toBe('future');
  });

  it('calculates transitioned states and terminal bypasses correctly', () => {
    const history: HistoryItem[] = [
      {
        id: '1',
        instanceId: 'inst-1',
        action: 'Started',
        timestamp: new Date().toISOString(),
        sequence: 1,
        toState: 'Start'
      },
      {
        id: '2',
        instanceId: 'inst-1',
        action: 'Transitioned',
        timestamp: new Date().toISOString(),
        sequence: 2,
        fromState: 'Start',
        toState: 'PendingManagerApproval'
      },
      {
        id: '3',
        instanceId: 'inst-1',
        action: 'Transitioned',
        timestamp: new Date().toISOString(),
        sequence: 3,
        fromState: 'PendingManagerApproval',
        toState: 'Completed'
      }
    ];

    // Inspect at sequence 2
    const snapshotAt2 = derivePlaybackSnapshot(history, 2, layout);
    expect(snapshotAt2.completedNodeIds).toContain('Start');
    expect(snapshotAt2.activeNodeIds).toContain('PendingManagerApproval');
    expect(snapshotAt2.nodeStates['PendingManagerApproval']).toBe('active');
    expect(snapshotAt2.nodeStates['Completed']).toBe('future');

    // Inspect at sequence 3 (terminal complete)
    const snapshotAt3 = derivePlaybackSnapshot(history, 3, layout);
    expect(snapshotAt3.completedNodeIds).toContain('Start');
    expect(snapshotAt3.completedNodeIds).toContain('PendingManagerApproval');
    expect(snapshotAt3.completedNodeIds).toContain('Completed');
    expect(snapshotAt3.activeNodeIds).toHaveLength(0);
    // Since it's terminal, other future nodes should be bypassed!
    expect(snapshotAt3.nodeStates['Rejected']).toBe('bypassed');
    expect(snapshotAt3.nodeStates['Cancelled']).toBe('bypassed');
  });
});
