# Phase 13: Interactive Debugging

The objective of Phase 13 is to build visual execution debugging and time-travel path analysis. When a developer inspects a workflow instance, they can scrub through the history timeline to see exactly how the state moved on the SVG visualizer, including the metadata, actors, and comment logs at each transition point.

---

## User Review Required

> [!IMPORTANT]
> **API, Database & Concurrency Architecture**
> - **No Silent Repository Retries**: In case of concurrency conflicts, the repository throws a typed `WorkflowConcurrencyException` and does not clear the aggregate's domain events. Retries must be managed at the application command-handler level by opening a new scope, reloading a fresh aggregate, validating business rules, generating new events, and committing.
> - **Internal Sequence Allocation**: `AllocateHistorySequence` is marked internal.
> - **Unit of Work Lifecycles**: Sequence mapping and history projection are managed by a `WorkflowCommitCoordinator` during the unit-of-work commit phase, not within the repository.
> - **Multi-Provider Migrations**: We will implement separate database migration and backfill scripts for SQL Server (using `ROW_NUMBER()`) and PostgreSQL.
> - **Visualizer Decoupling**: Build the playback logic in a new framework-neutral npm package `@arora/workflow-visualization`.

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
- Emit canonical node IDs directly from the execution engine to avoid guessing target nodes at persistence time:
  - `WorkflowStarted` includes `string InitialNodeId`.
  - `WorkflowTransitioned` includes `string FromNodeId` and `string ToNodeId`.
  - `WorkflowCancelled` includes `string LastActiveNodeId` and `string? CancelledNodeId` (dedicated terminal cancellation node).
- State/Node Mapping Invariant: Formalize in `docs/DDD.md` that state names and node IDs map one-to-one as canonical identifiers within the published definition version.

#### [MODIFY] WorkflowHistoryEntity.cs
- Add new properties representing the expanded database columns:
  ```csharp
  public long Sequence { get; set; }
  public string? NodeId { get; set; } // Nullable. Step-less events use null.
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
      public WorkflowConcurrencyException(Guid workflowInstanceId)
          : base(
              "WORKFLOW_CONCURRENCY_CONFLICT",
              $"Workflow instance '{workflowInstanceId}' was modified by another operation.")
      {
          WorkflowInstanceId = workflowInstanceId;
      }

      public Guid WorkflowInstanceId { get; }
  }
  ```

#### [NEW] WorkflowCommitCoordinator.cs
- Decouple commit coordination from repositories:
  - Intercepts pending domain events before saving.
  - Allocates sequences internally via `AllocateHistorySequence()`.
  - Runs the allowlist sanitizer to build event metadata.
  - Generates `WorkflowHistoryEntity` database rows.
  - Executes transaction commit.
  - Clears domain events strictly *after* successful database save.
  - Catches DbException/DbUpdateConcurrencyException and translates it into a `WorkflowConcurrencyException`.

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
Establish separate SQL migration and backfill scripts matching supported providers:
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
      public readonly code: "DUPLICATE_SEQUENCE" | "INVALID_SEQUENCE_ORDER" | "UNKNOWN_NODE" | "UNKNOWN_CONNECTION",
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
- **Concurrency Mutation Test**: Run concurrent operations against the same instance. Verify that one throws `WorkflowConcurrencyException`, aggregate events remain intact on the failed instance, and reloading the aggregate validates the state properly before retry.
- **Rollback Test**: Verify that a failed transaction persists neither the updated `HistorySequence` nor any projected history rows, does not clear the aggregate's domain events, and that any retry uses a freshly loaded aggregate or explicitly reset tracking state.
- **Consequential Events**: Multiple events committed in one transaction receive sequential sequences without gaps.
- **Sanitization Safe Clones**: Verify sanitization allows safe metadata and remains valid after context disposal.

#### Playback unit tests:
- Verify traversal count on cyclic loops (`A -> B -> A`).
- Verify error codes on duplicate or negative sequence orders.
- Verify node-less events work cleanly with null node IDs.
