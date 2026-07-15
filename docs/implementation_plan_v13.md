# Phase 13: Interactive Debugging

The objective of Phase 13 is to build visual execution debugging and time-travel path analysis. When a developer inspects a workflow instance, they can scrub through the history timeline to see exactly how the state moved on the SVG visualizer, including the input parameter metadata, actors, and comment logs at each transition point.

---

## User Review Required

> [!IMPORTANT]
> **API Schema Modifications**
> To support timeline scrubbing and transition comments, we will extend the `WorkflowHistoryItem` payload returned by `GET /instances/{id}/history` to include:
> - `Sequence` (long): Explicit sequential ordering key per instance, replacing time-based sorting.
> - `FromState` (string)
> - `ToState` (string)
> - `NodeId` (string): Stable graph-node identifier.
> - `ActorId` (string)
> - `ActorName` (string)
> - `Comment` (string)
> - `Metadata` (JSON dictionary): Redacted, structured key-value event properties.

---

## Proposed Changes

### 1. SDK Database & Core Extensions (C# Backend)

We will modify the core entities, query models, repository event projections, and entity configurations.

#### [MODIFY] WorkflowHistoryEntity.cs
- Add new columns/properties:
  ```csharp
  public long Sequence { get; set; }
  public string? NodeId { get; set; }
  ```

#### [MODIFY] ApprovalAndHistoryConfigurations.cs
- Configure `Sequence` column and update indexes on `WorkflowHistoryEntity` to use `Sequence` as the primary sorting criteria.

#### [MODIFY] EfCoreWorkflowInstanceRepository.cs
- In `SaveAsync`, calculate the current maximum sequence number per instance using a DB query.
- For each projected domain event, increment the sequence counter and persist it sequentially to ensure integrity.
- Map `NodeId` during event projection:
  - `WorkflowStarted` → `initialState.Name`
  - `WorkflowTransitioned` → `e.ToState`
  - `WorkflowCancelled` → `currentState`

#### [MODIFY] WorkflowHistoryItem.cs
- Update the API record contract:
  ```csharp
  public sealed record WorkflowHistoryItem(
      Guid Id,
      Guid InstanceId,
      long Sequence,
      string EventType,
      string? StepName,
      DateTimeOffset OccurredAt,
      string? ActorId,
      string? ActorName,
      string? FromState,
      string? ToState,
      string? Comment,
      string? NodeId,
      System.Text.Json.JsonElement? Metadata);
  ```

#### [MODIFY] EfCoreWorkflowQueryService.cs
- Project all extended fields from `WorkflowHistoryEntity` into `WorkflowHistoryItem` models, ordering by `Sequence` ascending.

---

### 2. Frontend Framework-Neutral Core (`@arora/workflow-react`)

We will implement the pure snapshot playback function in a new module.

#### [NEW] snapshotDeriver.ts
- Create a pure, unit-testable traversal function:
  ```typescript
  export interface WorkflowExecutionSnapshot {
    selectedSequence: number | null;
    activeNodeId?: string;
    completedNodeIds: string[];
    failedNodeIds: string[];
    traversedConnections: string[];
    selectedEvent?: WorkflowHistoryItem;
  }

  export function deriveExecutionSnapshot(
    layout: WorkflowLayout | null,
    history: WorkflowHistoryItem[],
    selectedSequence: number | null
  ): WorkflowExecutionSnapshot;
  ```
- Algorithms:
  - If `selectedSequence` is `null` (Live mode), compile snapshot based on the current state of the instance.
  - If a sequence index is selected, walk history items sequentially up to the selected item:
    - Mark transitions (`FromState` -> `ToState`) as traversed lines.
    - Set final item's `NodeId` as `activeNodeId`.
    - Mark intermediate historical nodes as completed.

---

### 3. React UI Components (`@arora/workflow-react`)

#### [MODIFY] HistoryTimeline.tsx
- Add a callback prop `onSelectHistoryItem?: (item: WorkflowHistoryItem | null) => void`.
- Enhance rows to use `<button>` elements with strict accessibility (`role="button"`, key listener overrides for ArrowUp, ArrowDown, Space, Enter, and clear focus styling).

#### [MODIFY] InstanceDetailsView.tsx
- Hold state for `selectedHistoryItem` (default `null` representing Live mode).
- If selection is active, render a **Historical Mode Banner** indicating: `"Viewing workflow as of event X of Y. [Return to Live]"`
- Pass layout and history to `deriveExecutionSnapshot` and forward derived coordinate arrays to `<WorkflowVisualizer />`.
- Render an **Inspector Panel** showing comments, actor identity details, and structured event metadata.

---

## Verification Plan

### Automated Tests

#### Backend unit tests:
- Verify sequence number projection: starting at 1, strictly sequential, and partitioned correctly by instance.
- Verify tenant isolation on history queries.
- Verify metadata column nullability.

#### Frontend unit tests (`snapshotDeriver.test.ts`):
- Verify path traversal output on:
  - Initial step (Started).
  - Normal path transition.
  - Cycle loops and repeating nodes.
  - Cancelled/failed steps.

#### Component testing:
- Keyboard navigation (arrows/space/enter) on timeline rows.
- Banner display and "Return to Live" click action.
- Clearing state when switching instance selection.
