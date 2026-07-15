# Phase 13: Interactive Debugging

The objective of Phase 13 is to build visual execution debugging and time-travel path analysis. When a developer inspects a workflow instance, they can scrub through the history timeline to see exactly how the state moved on the SVG visualizer, including the metadata, actors, and comment logs at each transition point.

---

## User Review Required

> [!IMPORTANT]
> **API, Database & Concurrency Architecture**
> - **DbUpdateConcurrencyException Translation**: Only translate `DbUpdateConcurrencyException` and the unique index sequence conflict `uq_aw_workflow_history_tenant_instance_sequence` into `WorkflowConcurrencyException`. Arbitrary database connection, schema, or timeout failures will bypass concurrency retries and throw standard infrastructure errors.
> - **Application-Level Command Retries**: Failsafe aggregate retries must discard the failed scope, load a fresh aggregate, and re-execute the business command to raise fresh events. Stale event lists must never be reused or copied.
> - **Post-Commit Event Dispatch**: Separate the atomic database transaction (commit state + history) from post-commit MediatR dispatch, ensuring handlers run only after successful commit.
> - **State vs. Node Distinction (Option B)**: Formally declare in `docs/DDD.md` that States (e.g. `PendingManagerApproval`) and Graph Node IDs (e.g. `manager-approval`) are distinct concepts. Domain events will carry both references.
> - **Multi-Aggregate Commits**: The commit coordinator supports multiple `WorkflowInstance` aggregates modified in the same transaction, partitioning event arrays and sequence counters per-instance.

---

## Proposed Changes

### 1. Domain Models & Core Engine (C# Backend)

We will modify the core aggregates, configurations, and domain events.

#### [MODIFY] WorkflowInstance.cs
- Add `HistorySequence` state property and mark the allocation methods as internal:
  ```csharp
  public long HistorySequence { get; private set; }

  internal void SetHistorySequence(long sequence) => HistorySequence = sequence;

  internal long AllocateHistorySequence()
  {
      HistorySequence++;
      return HistorySequence;
  }
  ```

#### [MODIFY] WorkflowEvents.cs (Domain Events)
- Emit canonical state names and graph node IDs directly from the execution engine:
  - `WorkflowStarted`: includes `string InitialState` and `string InitialNodeId`.
  - `WorkflowTransitioned`: includes `string FromState`, `string ToState`, `string FromNodeId`, and `string ToNodeId`.
  - `WorkflowCancelled`: includes `string LastActiveState`, `string LastActiveNodeId`, and `string? CancelledNodeId`.
- Document these distinct semantics inside `docs/DDD.md`.

#### [MODIFY] WorkflowHistoryEntity.cs
- Add new properties representing the expanded database columns:
  ```csharp
  public long Sequence { get; set; }
  public string? NodeId { get; set; } // Nullable. Node-less events use null.
  ```

#### [MODIFY] ApprovalAndHistoryConfigurations.cs
- Configure unique index constraint on `WorkflowHistoryEntity`:
  ```csharp
  builder.HasIndex(x => new { x.TenantId, x.WorkflowInstanceId, x.Sequence })
      .IsUnique()
      .HasDatabaseName("uq_aw_workflow_history_tenant_instance_sequence");
  ```

---

### 2. Transaction Coordinator & Concurrency Exceptions

#### [NEW] WorkflowConcurrencyException.cs
- Implement a typed concurrency exception:
  ```csharp
  public sealed class WorkflowConcurrencyException : WorkflowException
  {
      public WorkflowConcurrencyException(Guid workflowInstanceId, Exception? innerException = null)
          : base(
              "WORKFLOW_CONCURRENCY_CONFLICT",
              $"Workflow instance '{workflowInstanceId}' was modified by another operation.",
              innerException)
      {
          WorkflowInstanceId = workflowInstanceId;
      }

      public Guid WorkflowInstanceId { get; }
  }
  ```

#### [NEW] WorkflowCommitCoordinator.cs
- Decouple commit coordination from repositories:
  - Collects pending domain events from all dirty aggregates in the transaction.
  - Groups events by `WorkflowInstanceId` and allocates sequences per-instance.
  - Runs allowlist sanitization on event payload metadata.
  - Projects `WorkflowHistoryEntity` entities.
  - Executes database transaction commit.
  - Clears domain events from aggregates strictly *after* successful commit.
  - Traps `DbUpdateConcurrencyException` or unique constraint conflicts on `uq_aw_workflow_history_tenant_instance_sequence` and translates them to `WorkflowConcurrencyException`.
  - Bypasses translation for standard connection outages, timeouts, and foreign-key errors.
  - Dispatches MediatR/domain events to external listeners only after the database transaction has succeeded.

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

### 4. Database Migrations (SQL Server & PostgreSQL)
Configure migrations in their respective provider projects (`Arora.Workflow.EntityFramework.SqlServer` and `Arora.Workflow.EntityFramework.PostgreSql`):
- **SQL Server**:
  - Add columns, then backfill sequence:
    ```sql
    WITH Backfill AS (
      SELECT Id, ROW_NUMBER() OVER(PARTITION BY TenantId, WorkflowInstanceId ORDER BY OccurredAt, Id) AS NewSeq
      FROM aw_workflow_history
    )
    UPDATE h
    SET h.Sequence = b.NewSeq
    FROM aw_workflow_history h
    JOIN Backfill b ON h.Id = b.Id;
    ```
- **PostgreSQL**:
  - Implement a corresponding sequence backfill using `row_number() over (PARTITION BY ...)` syntax.
- Update instance history sequence counters based on maximum historical value found.

---

### 5. Playback Core Package (`@arora/workflow-visualization`)

Build a dedicated, framework-neutral playback package.

#### [NEW] @arora/workflow-visualization/src/snapshotDeriver.ts
- Declare playback model interfaces supporting loops and node outcomes:
  ```typescript
  export type WorkflowNodeStatus = "pending" | "completed" | "active" | "failed" | "rejected" | "cancelled";

  export interface WorkflowNodeExecution {
    nodeId: string;
    visitCount: number;
    lastSequence: number;
    status: WorkflowNodeStatus;
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

  export class WorkflowHistoryIntegrityError extends Error {
    constructor(
      public readonly code:
        | "DUPLICATE_SEQUENCE"
        | "INVALID_SEQUENCE_ORDER"
        | "MISSING_SEQUENCE"
        | "INVALID_SEQUENCE"
        | "UNKNOWN_NODE"
        | "UNKNOWN_CONNECTION",
      message: string
    ) {
      super(message);
    }
  }

  export function deriveExecutionSnapshot(
    layout: WorkflowLayout | null,
    history: WorkflowHistoryItem[],
    selectedSequence: number | null,
    currentNodeId?: string
  ): WorkflowExecutionSnapshot;
  ```
- Playback Resolution Logic:
  - If a sequence is selected (Historical mode):
    - Walk historical events strictly up to and including `selectedSequence`.
    - Nodes with terminal events (`WorkflowCompleted`, `WorkflowCancelled`, `WorkflowRejected`) are marked with their terminal status (`"completed"`, `"cancelled"`, `"rejected"`); they do not display as `"active"`.
    - Node-less events (null `NodeId`): remain selectable in the timeline, update the metadata panel, but do not change the active visualizer node or throw `UNKNOWN_NODE`.

---

### 6. React Timeline A11y & UI

#### [MODIFY] HistoryTimeline.tsx
- Render steps using native `<button>` tags.
- Manage logical index focus using keyboard ArrowUp/ArrowDown listeners.
- Render banner indicators (`"Viewing event X of Y. [Return to live state]"`) and manage focus shifts when switching instances.

---

## Verification Plan

### Automated Tests

#### Backend Integration Tests (SQL Server & PostgreSQL Testcontainers):
- **Concurrency Conflict Resolution**: Spawn concurrent approve and reject commands. Verify that one succeeds and the other throws `WorkflowConcurrencyException`. Re-executing the losing command against the reloaded aggregate yields a predictable business outcome (e.g. `InvalidTransitionException` or `DuplicateApprovalException`) rather than blindly applying the stale state modification.
- **Rollback Consistency**: Verify that a failed transaction persists neither the updated `HistorySequence` nor any projected history rows, does not clear the aggregate's domain events, and that any retry uses a freshly loaded aggregate or explicitly reset tracking state.
- **Consequential Events & Gaps**: Multiple events committed in one transaction receive sequential sequences without gaps.
- **Multi-Aggregate Scope**: Modify two separate workflow instances in a single unit of work. Verify they maintain independent sequences and separate history counters.
- **Standard Error Outage Bypasses**: Bypasses connection, unique index, and foreign key errors from translating into concurrency exceptions.

#### Playback unit tests:
- Verify traversal count on cyclic loops (`A -> B -> A`).
- Verify integrity error throwing for missing, negative, zero, or duplicate sequences.
- Verify node-less events work cleanly with null node IDs.
- Verify terminal node playback displays the final event state (`completed`, `rejected`, `cancelled`) instead of highlighting the node as `active`.
