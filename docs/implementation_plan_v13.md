# Phase 13: Interactive Debugging

The objective of Phase 13 is to build visual execution debugging and time-travel path analysis. When a developer inspects a workflow instance, they can scrub through the history timeline to see exactly how the state moved on the SVG visualizer, including the metadata, actors, and comment logs at each transition point.

---

## User Review Required

> [!IMPORTANT]
> **API & Architecture Changes**
> - **In-Memory Concurrency & Counter Recovery**: Domain events are cleared only *after* a successful commit. If saving fails, projected history entities are discarded, the aggregate and counter are reloaded, and history events are re-projected before retrying.
> - **Shared Visualizer Package**: Creation of a new package `@arora/workflow-visualization` containing the pure snapshot playback derivation engine.
> - **Nullable NodeId**: `NodeId` remains nullable to allow for step-less events (retry schedules, deadlines, failed notifications).
> - **EF Core Migrations**: Shift from prototype `EnsureCreated()` to a structured EF Core migration and backfill script.
> - **Relational Testcontainers**: Integration tests will use Testcontainers (PostgreSQL/xmin or SQL Server/rowversion) to verify concurrency locks.

---

## Proposed Changes

### 1. Backend Core & Database (C# Backend)

We will modify the core aggregate, query models, repository event projections, and entity configurations.

#### [MODIFY] WorkflowInstance.cs
- Add `HistorySequence` state property:
  ```csharp
  public long HistorySequence { get; private set; }

  internal void SetHistorySequence(long sequence) => HistorySequence = sequence;

  public long AllocateHistorySequence()
  {
      HistorySequence++;
      return HistorySequence;
  }
  ```

#### [MODIFY] WorkflowEvents.cs (Domain Events)
- Emit canonical node IDs directly from the execution engine to avoid guessing target nodes at persistence time:
  - `WorkflowStarted` includes `string InitialNodeId`.
  - `WorkflowTransitioned` includes `string FromNodeId` and `string ToNodeId`.
  - `WorkflowCancelled` includes `string LastActiveNodeId`.

#### [MODIFY] WorkflowHistoryEntity.cs
- Add new properties representing the expanded table columns:
  ```csharp
  public long Sequence { get; set; }
  public string? NodeId { get; set; } // Nullable. Not all events map to nodes.
  ```

#### [MODIFY] ApprovalAndHistoryConfigurations.cs
- Add unique index constraint:
  ```csharp
  builder.HasIndex(x => new { x.TenantId, x.WorkflowInstanceId, x.Sequence })
      .IsUnique()
      .HasDatabaseName("uq_aw_workflow_history_tenant_instance_sequence");
  ```

#### [MODIFY] EfCoreWorkflowInstanceRepository.cs
- Re-architect saving sequence numbers under retry/rollback scopes:
  - Domain events are cleared **only after a successful commit**.
  - On `SaveChangesAsync` concurrency failure, projected `WorkflowHistoryEntity` entities are discarded/detached from EF tracking.
  - Re-read the aggregate and latest `HistorySequence` from the database, re-assign sequence numbers, and re-project history entities before executing the retry loop.

---

### 2. Metadata Sanitization

#### [NEW] IWorkflowHistoryMetadataSanitizer.cs
- Define the interface for metadata sanitization:
  ```csharp
  public record WorkflowHistoryMetadataContext(
      Guid TenantId,
      Guid WorkflowInstanceId,
      string EventType,
      string? StepName);

  public interface IWorkflowHistoryMetadataSanitizer
  {
      System.Text.Json.JsonElement? Sanitize(
          IWorkflowEvent domainEvent,
          WorkflowHistoryMetadataContext context);
  }
  ```
- Create `DefaultWorkflowHistoryMetadataSanitizer` implementing a strict **allowlist**:
  - Keep: statuses, attempts, approval decisions, elapsed durations.
  - Redact/Exclude: raw input/output parameter payloads, authorization headers/tokens, exception stack traces.
  - Enforce limits: maximum serialized metadata size, string length, and nesting depth.
  - Clone returning values (`sanitizedElement.Clone()`) to prevent JSON document disposal issues.

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
      string? NodeId, // Nullable.
      System.Text.Json.JsonElement? Metadata);
  ```

#### [MODIFY] EfCoreWorkflowQueryService.cs
- Query `WorkflowHistoryEntity` ordering strictly by `Sequence` ascending. Map all properties into `WorkflowHistoryItem`.

---

### 4. Database Migration Plan (EF Core Migrations)
Establish concrete schema migration script representing:
1. Add nullable `Sequence` (long) and `NodeId` (string?) columns on the history table.
2. Add `HistorySequence` (long) to the instance table, defaulting to 0.
3. Backfill script: For each instance, sort existing rows by `OccurredAt` then `Id` and update `Sequence` with incrementing sequences (1, 2, 3...). Set `HistorySequence` in the instance table to the maximum assigned sequence.
4. Alter `Sequence` column on the history table to be non-nullable.
5. Create unique constraint index on `(TenantId, WorkflowInstanceId, Sequence)`.

---

### 5. Playback & Core Visualization Logic (`@arora/workflow-visualization`)

We will place framework-neutral playback logic inside a new dedicated workspace package.

#### [NEW] @arora/workflow-visualization/src/snapshotDeriver.ts
- Create snapshot model interfaces supporting looping states:
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
- Implement keyboard navigation for `ArrowUp` and `ArrowDown` keys to move focus.
- Set `aria-current="step"` on the currently selected historical event row.

#### [MODIFY] InstanceDetailsView.tsx
- Hold state for `selectedHistoryItem` (defaults to `null` representing Live mode).
- If selection is active, show the **Historical Mode Alert Banner** (`"Viewing workflow as of event X. [Return to Live]"`).
- Invoke `deriveExecutionSnapshot` from `@arora/workflow-visualization` and forward computed classes to `<WorkflowVisualizer />`.
- Render the transition comment, sanitizer metadata, and actor display logs in the details sidebar.

---

## Verification Plan

### Automated Tests

#### Backend integration tests (using SQL Server Testcontainers):
- **Concurrency Test**: Spawn concurrent scopes updating the same instance. Verify that the concurrency token on the aggregate throws a conflict, discards generated history entities, reloads `HistorySequence`, re-projects the events, and succeeds on retry.
- **Rollback Test**: Verify that a failed transaction rolls back both the instance `HistorySequence` state counter and history logs.
- **Unique Index Test**: Verify that database unique constraint throws if duplicate sequences are injected.
- **Sanitization Test**: Verify that `DefaultWorkflowHistoryMetadataSanitizer` enforces size limits and strips raw exception traces and inputs.

#### Playback Unit Tests (`snapshotDeriver.test.ts`):
- Loop traversal resolution: Verify correct `visitCount` and `traversalCount` for workflows going through `A -> B -> A`.
- Live sync: Verify snapshot behavior when live `currentNodeId` is different from the last history row.
- Cancellation highlight: Verify cancellation nodes are marked `"cancelled"`.
- Error recovery: Verify playback behavior when sequences have missing elements or duplicate sequence inputs are rejected explicitly.

#### Component & A11y tests:
- Timeline Keyboard navigation: Triggering Up/Down focus changes.
- Safe rendering: Verify that comments are rendered strictly as text nodes, protecting against HTML injection.
