# Phase 13: Interactive Debugging

The objective of Phase 13 is to build visual execution debugging and time-travel path analysis. When a developer inspects a workflow instance, they can scrub through the history timeline to see exactly how the state moved on the SVG visualizer, including the metadata, actors, and comment logs at each transition point.

---

## User Review Required

> [!IMPORTANT]
> **Database Constraints & Schema Migration**
> - **HistorySequence Property**: Added to `WorkflowInstance` to act as an atomic counter for event sequencing.
> - **Unique Constraint**: Added `UNIQUE (TenantId, WorkflowInstanceId, Sequence)` on the history table to prevent duplicates under concurrency.
> - **Metadata Sanitization**: Added the `IWorkflowHistoryMetadataSanitizer` abstraction to explicitly strip out PII, secrets, and large payloads before event logging.

---

## Proposed Changes

### 1. SDK Domain & Database Extensions (C# Backend)

We will modify the core aggregate, configuration tables, and event projection models.

#### [MODIFY] WorkflowInstance.cs
- Add `HistorySequence` state property:
  ```csharp
  public long HistorySequence { get; private set; }

  public long AllocateHistorySequence()
  {
      HistorySequence++;
      return HistorySequence;
  }
  ```
- Update factory initialization to start `HistorySequence = 0`.

#### [MODIFY] WorkflowEvents.cs (Domain Events)
- Emit canonical node IDs directly from the execution engine to avoid guessing target nodes at persistence time:
  - `WorkflowStarted` includes `string InitialNodeId`.
  - `WorkflowTransitioned` includes `string FromNodeId` and `string ToNodeId`.
  - `WorkflowCancelled` includes `string LastActiveNodeId`.

#### [MODIFY] WorkflowHistoryEntity.cs
- Add new properties representing the expanded table columns:
  ```csharp
  public long Sequence { get; set; }
  public string? NodeId { get; set; }
  ```

#### [MODIFY] ApprovalAndHistoryConfigurations.cs
- Add unique index constraint:
  ```csharp
  builder.HasIndex(x => new { x.TenantId, x.WorkflowInstanceId, x.Sequence })
      .IsUnique()
      .HasDatabaseName("uq_aw_workflow_history_tenant_instance_sequence");
  ```

#### [MODIFY] EfCoreWorkflowInstanceRepository.cs
- During saving operations inside `SaveAsync`, project events into `WorkflowHistoryEntity` using sequence numbers allocated from the instance aggregate:
  ```csharp
  foreach (var domainEvent in domainEvents)
  {
      var sequence = instance.AllocateHistorySequence();
      historyEntities.Add(MapToEntity(domainEvent, sequence));
  }
  ```
- Ensure the instance's updated `HistorySequence` is saved concurrently under the database transaction.

---

### 2. Metadata Sanitization

#### [NEW] IWorkflowHistoryMetadataSanitizer.cs
- Define the interface for metadata sanitization:
  ```csharp
  public interface IWorkflowHistoryMetadataSanitizer
  {
      System.Text.Json.JsonElement? Sanitize(
          object domainEvent,
          Guid tenantId,
          Guid instanceId);
  }
  ```
- Create a default `DefaultWorkflowHistoryMetadataSanitizer` that:
  - Preserves safe properties: statuses, attempts, approval decisions, elapsed durations.
  - Strips/redacts: raw workflow payloads (input/output parameters), exception stack traces, authentication headers.
- Register it in dependency injection.

---

### 3. API Model Extension

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
- Query `WorkflowHistoryEntity` ordering strictly by `Sequence` ascending. Map all properties into `WorkflowHistoryItem`.

---

### 4. Database Migration Plan
Even though `EnsureCreated()` is utilized, we will document the schema migration strategy for backfills:
1. Add `Sequence` and `NodeId` as nullable columns on the `aw_workflow_history` table.
2. Backfill existing records: Sort by `OccurredAt` then by `Id` per instance, and update `Sequence` sequentially (1, 2, 3...). Resolve `NodeId` using state maps.
3. Update `aw_workflow_instances` table to populate the `HistorySequence` column with the max sequence number found in history.
4. Alter `Sequence` and `NodeId` to be non-nullable.
5. Create unique constraint index on `(TenantId, WorkflowInstanceId, Sequence)`.

---

### 5. Playback & Core Visualization Logic (`@arora/workflow-client`)

We will place framework-neutral playback logic inside the client core to ensure future Angular reuse.

#### [NEW] client/src/playback/snapshotDeriver.ts
- Create snapshot model interfaces support looping states:
  ```typescript
  export interface WorkflowNodeExecution {
    nodeId: string;
    visitCount: number;
    lastSequence: number;
    status: "completed" | "active" | "failed" | "cancelled";
  }

  export interface WorkflowConnectionExecution {
    connectionId: string;
    traversalCount: number;
    lastSequence: number;
  }

  export interface WorkflowExecutionSnapshot {
    selectedSequence: number | null;
    nodes: Record<string, WorkflowNodeExecution>;
    connections: Record<string, WorkflowConnectionExecution>;
    selectedEvent?: WorkflowHistoryItem;
  }

  export function deriveExecutionSnapshot(
    layout: WorkflowLayout | null,
    history: WorkflowHistoryItem[],
    selectedSequence: number | null,
    currentNodeId?: string
  ): WorkflowExecutionSnapshot;
  ```
- Playback Resolution Logic:
  - If `selectedSequence` is `null` (Live mode):
    - Walk the complete history to compute traversals, marking node visits.
    - Set the status of `currentNodeId` as `"active"`.
  - If a sequence is selected (Historical mode):
    - Walk historical events strictly up to and including `selectedSequence`.
    - Track node visits and transition counters. The node corresponding to `selectedSequence` is marked `"active"`. All prior visited nodes are `"completed"`.
    - If the selected event represents a cancellation, mark the target node as `"cancelled"`.

---

### 6. React UI Components (`@arora/workflow-react`)

#### [MODIFY] HistoryTimeline.tsx
- Render timeline steps as accessible native `<button>` tags (retaining native Space/Enter selection functionality).
- Implement keyboard navigation for `ArrowUp` and `ArrowDown` keys to move logical focus between rows.
- Set `aria-current="step"` on the currently selected historical event row.

#### [MODIFY] InstanceDetailsView.tsx
- Hold state for `selectedHistoryItem` (defaults to `null` representing Live mode).
- If selection is active, show the **Historical Mode Alert Banner** (`"Viewing workflow as of event X. [Return to Live]"`).
- Invoke `deriveExecutionSnapshot` and forward computed layout classes to `<WorkflowVisualizer />`.
- Render the transition comment, sanitizer metadata, and actor display logs in the details sidebar.

---

## Verification Plan

### Automated Tests

#### Backend unit tests:
- **Concurrency Test**: Simulate two concurrent writes to the same instance using parallel threads to verify that database optimistic concurrency tokens block double sequence allocations.
- **Rollback Test**: Verify that a failed transaction rolls back both the instance `HistorySequence` state counter and history logs.
- **Unique Constraint**: Verify that database writes fail if duplicate sequences are injected.
- **Sanitization Test**: Verify that `DefaultWorkflowHistoryMetadataSanitizer` strips out custom parameters while logging safe numbers.

#### Playback Unit Tests (`snapshotDeriver.test.ts`):
- Loop traversal resolution: Verify correct `visitCount` and `traversalCount` for workflows going through `A -> B -> A`.
- Live sync: Verify snapshot behavior when live `currentNodeId` is different from the last history row.
- Cancellation highlight: Verify cancellation nodes are marked `"cancelled"`.

#### Component & A11y tests:
- Timeline Keyboard navigation: Triggering Up/Down focus changes.
- Safe rendering: Verify that comments are rendered strictly as text nodes, protecting against HTML injection.
