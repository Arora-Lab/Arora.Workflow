# Database Design
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01

---

## 1. Design Philosophy

The database is the source of truth for all workflow state. This is Principle 1 of the Arora.Workflow architecture, and every schema decision flows from it.

**Consequences of this philosophy:**

- Workflow state is never stored only in application memory or a distributed cache.
- Every state transition is persisted before it is considered complete.
- The audit history (`WorkflowHistory`) is a first-class table, not a log file.
- All tables include `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` audit columns.
- Soft delete (`IsDeleted`, `DeletedAt`) is used instead of hard delete for `WorkflowDefinitions` and `WorkflowInstances`.
- All tables carry a `TenantId` column, enforced by EF Core Global Query Filters.

---

## 2. Schema Overview

```
WorkflowDefinitions
       │
       ├── WorkflowStepDefinitions
       │        │
       │        └── EscalationPolicies
       │
WorkflowInstances ──── WorkflowDefinitions (FK)
       │
       ├── StepResults
       │
       ├── Approvals ──── EscalationPolicies (FK)
       │
       ├── WorkflowHistory
       │
       └── WorkflowDeadlines
```

---

## 3. Table Definitions

### `WorkflowDefinitions`

Stores published (and draft) workflow blueprints.

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `TenantId` | `uuid` | NOT NULL | — | Global Query Filter |
| `Name` | `varchar(200)` | NOT NULL | — | Unique per tenant + version |
| `Version` | `int` | NOT NULL | — | Monotonically increasing per `Name` |
| `Description` | `text` | NULL | — | Human-readable description |
| `Status` | `varchar(20)` | NOT NULL | `'Draft'` | `Draft`, `Published`, `Deprecated` |
| `DefinitionJson` | `jsonb` | NOT NULL | — | Full step/transition graph serialized |
| `IsDeleted` | `bool` | NOT NULL | `false` | Soft delete |
| `DeletedAt` | `timestamptz` | NULL | — | Set when `IsDeleted = true` |
| `CreatedAt` | `timestamptz` | NOT NULL | `now()` | |
| `CreatedBy` | `varchar(200)` | NOT NULL | — | Actor ID who created |
| `ModifiedAt` | `timestamptz` | NOT NULL | `now()` | |
| `ModifiedBy` | `varchar(200)` | NOT NULL | — | Actor ID who last modified |
| `RowVersion` | `bytea` / `xmin` | NOT NULL | — | Optimistic concurrency |

**Indexes:**
- `PRIMARY KEY (Id)`
- `UNIQUE (TenantId, Name, Version)` — prevents duplicate versions
- `INDEX (TenantId, Status)` — filter by active definitions

---

### `WorkflowStepDefinitions`

Stores individual step definitions that belong to a `WorkflowDefinition`. Normalized out of the `DefinitionJson` for queryability.

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `TenantId` | `uuid` | NOT NULL | — | |
| `WorkflowDefinitionId` | `uuid` | NOT NULL | — | FK → `WorkflowDefinitions.Id` |
| `Name` | `varchar(200)` | NOT NULL | — | Unique within the definition |
| `StepType` | `varchar(50)` | NOT NULL | — | `Standard`, `Approval` |
| `ImplementationType` | `varchar(500)` | NOT NULL | — | Assembly-qualified type name |
| `InputSchema` | `jsonb` | NULL | — | JSON Schema for step input |
| `RetryPolicyJson` | `jsonb` | NULL | — | Serialized `RetryPolicy` |
| `Order` | `int` | NOT NULL | — | Execution order within the definition |

**Indexes:**
- `PRIMARY KEY (Id)`
- `INDEX (WorkflowDefinitionId)` — fetch all steps for a definition

---

### `EscalationPolicies`

Escalation rules attached to Approval step definitions.

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `TenantId` | `uuid` | NOT NULL | — | |
| `WorkflowStepDefinitionId` | `uuid` | NOT NULL | — | FK → `WorkflowStepDefinitions.Id` |
| `DeadlineDurationSeconds` | `int` | NOT NULL | — | Duration before escalation fires |
| `EscalationActorId` | `varchar(200)` | NULL | — | Specific actor to escalate to |
| `EscalationRole` | `varchar(200)` | NULL | — | Role to escalate to (either/or with actor) |
| `EscalationAction` | `varchar(50)` | NOT NULL | `'Escalate'` | `Escalate`, `AutoReject`, `AutoApprove` |

---

### `WorkflowInstances`

The central execution record. One row per running or completed workflow.

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `TenantId` | `uuid` | NOT NULL | — | Global Query Filter |
| `WorkflowDefinitionId` | `uuid` | NOT NULL | — | FK → `WorkflowDefinitions.Id` |
| `WorkflowDefinitionVersion` | `int` | NOT NULL | — | Snapshot of version at start time |
| `CorrelationId` | `varchar(500)` | NOT NULL | — | Business entity reference (e.g. invoice ID) |
| `CurrentState` | `varchar(200)` | NOT NULL | — | Current state name |
| `Status` | `varchar(50)` | NOT NULL | `'Running'` | `Running`, `PendingApproval`, `Completed`, `Rejected`, `Cancelled` |
| `InputJson` | `jsonb` | NULL | — | Serialized workflow input payload |
| `IdempotencyKey` | `varchar(500)` | NOT NULL | — | Caller-provided; prevents duplicate starts |
| `IsDeleted` | `bool` | NOT NULL | `false` | Soft delete |
| `DeletedAt` | `timestamptz` | NULL | — | |
| `CompletedAt` | `timestamptz` | NULL | — | Set when terminal state reached |
| `CreatedAt` | `timestamptz` | NOT NULL | `now()` | |
| `CreatedBy` | `varchar(200)` | NOT NULL | — | Actor ID who started the workflow |
| `ModifiedAt` | `timestamptz` | NOT NULL | `now()` | |
| `ModifiedBy` | `varchar(200)` | NOT NULL | — | |
| `RowVersion` | `bytea` / `xmin` | NOT NULL | — | Optimistic concurrency |

**Indexes:**
- `PRIMARY KEY (Id)`
- `UNIQUE (TenantId, IdempotencyKey)` — idempotent start enforcement
- `UNIQUE (TenantId, CorrelationId, WorkflowDefinitionId)` — one active instance per entity+definition
- `INDEX (TenantId, Status)` — hot query: all pending instances
- `INDEX (TenantId, WorkflowDefinitionId, Status)` — hot query: pending instances of a given workflow type

---

### `StepResults`

Records each execution attempt of a step within a workflow instance.

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `TenantId` | `uuid` | NOT NULL | — | |
| `WorkflowInstanceId` | `uuid` | NOT NULL | — | FK → `WorkflowInstances.Id` |
| `StepName` | `varchar(200)` | NOT NULL | — | |
| `StepExecutionId` | `uuid` | NOT NULL | — | Idempotency key for this execution attempt |
| `AttemptNumber` | `int` | NOT NULL | `1` | Increments on retry |
| `Status` | `varchar(50)` | NOT NULL | — | `Succeeded`, `Failed`, `Skipped` |
| `OutputJson` | `jsonb` | NULL | — | Serialized step output |
| `ErrorJson` | `jsonb` | NULL | — | Exception detail on failure |
| `DurationMs` | `int` | NOT NULL | — | Step execution wall-clock time |
| `StartedAt` | `timestamptz` | NOT NULL | — | |
| `CompletedAt` | `timestamptz` | NOT NULL | — | |
| `CreatedAt` | `timestamptz` | NOT NULL | `now()` | |

**Indexes:**
- `PRIMARY KEY (Id)`
- `UNIQUE (TenantId, StepExecutionId)` — idempotency enforcement
- `INDEX (WorkflowInstanceId)` — fetch all step results for an instance

---

### `Approvals`

Pending and decided approval records for Approval Steps.

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `TenantId` | `uuid` | NOT NULL | — | |
| `WorkflowInstanceId` | `uuid` | NOT NULL | — | FK → `WorkflowInstances.Id` |
| `StepName` | `varchar(200)` | NOT NULL | — | The Approval Step name |
| `Status` | `varchar(50)` | NOT NULL | `'Pending'` | `Pending`, `Approved`, `Rejected`, `Escalated`, `Withdrawn` |
| `AssignedActorId` | `varchar(200)` | NOT NULL | — | Who is expected to decide |
| `AssignedActorName` | `varchar(500)` | NOT NULL | — | Display name (denormalized for history) |
| `DecisionActorId` | `varchar(200)` | NULL | — | Who actually decided (may differ after escalation) |
| `DecisionActorName` | `varchar(500)` | NULL | — | |
| `Comment` | `text` | NULL | — | Optional actor comment |
| `EscalationPolicyId` | `uuid` | NULL | — | FK → `EscalationPolicies.Id` |
| `DeadlineAt` | `timestamptz` | NULL | — | Computed: `CreatedAt + EscalationPolicy.Duration` |
| `DecidedAt` | `timestamptz` | NULL | — | Set when decision is submitted |
| `CreatedAt` | `timestamptz` | NOT NULL | `now()` | |

**Indexes:**
- `PRIMARY KEY (Id)`
- `INDEX (TenantId, Status)` — hot query: all pending approvals across tenant
- `INDEX (TenantId, AssignedActorId, Status)` — hot query: approvals assigned to a specific actor
- `INDEX (WorkflowInstanceId)` — approvals for a specific workflow instance

---

### `WorkflowHistory`

Immutable audit log. Append-only. Never updated, never deleted.

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `TenantId` | `uuid` | NOT NULL | — | |
| `WorkflowInstanceId` | `uuid` | NOT NULL | — | FK → `WorkflowInstances.Id` |
| `EventType` | `varchar(100)` | NOT NULL | — | e.g. `WorkflowStarted`, `ApprovalGranted` |
| `FromState` | `varchar(200)` | NULL | — | State before transition (null for initial) |
| `ToState` | `varchar(200)` | NULL | — | State after transition |
| `StepName` | `varchar(200)` | NULL | — | Step that produced this event |
| `ActorId` | `varchar(200)` | NULL | — | Who caused this event |
| `ActorName` | `varchar(500)` | NULL | — | Denormalized for readability |
| `Comment` | `text` | NULL | — | Actor comment (approvals, cancellations) |
| `Metadata` | `jsonb` | NULL | — | Event-specific structured data |
| `DurationMs` | `int` | NULL | — | Duration of the step/operation |
| `OccurredAt` | `timestamptz` | NOT NULL | `now()` | Immutable timestamp |

**Indexes:**
- `PRIMARY KEY (Id)`
- `INDEX (WorkflowInstanceId, OccurredAt)` — chronological history for an instance
- `INDEX (TenantId, OccurredAt)` — tenant-wide audit queries

---

### `WorkflowDeadlines`

Tracks pending escalation timers. Polled by the `EscalationScheduler`.

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `TenantId` | `uuid` | NOT NULL | — | |
| `WorkflowInstanceId` | `uuid` | NOT NULL | — | FK → `WorkflowInstances.Id` |
| `ApprovalId` | `uuid` | NOT NULL | — | FK → `Approvals.Id` |
| `FireAt` | `timestamptz` | NOT NULL | — | When the escalation should fire |
| `IsProcessed` | `bool` | NOT NULL | `false` | Set to true after escalation fires |
| `ProcessedAt` | `timestamptz` | NULL | — | |
| `CreatedAt` | `timestamptz` | NOT NULL | `now()` | |

**Indexes:**
- `PRIMARY KEY (Id)`
- `INDEX (IsProcessed, FireAt)` — hot query for the escalation poller

---

## 4. Multi-Tenancy Strategy

All tables carry a `TenantId` column. EF Core Global Query Filters are applied at `DbContext` configuration time:

```csharp
modelBuilder.Entity<WorkflowInstance>()
    .HasQueryFilter(x => x.TenantId == _tenantContext.CurrentTenantId && !x.IsDeleted);
```

This ensures every query, without exception, is scoped to the current tenant. No application code needs to add `WHERE TenantId = @id` manually. Bypassing the filter requires explicit `.IgnoreQueryFilters()` and is disallowed in production paths.

---

## 5. Soft Delete Strategy

`WorkflowDefinitions` and `WorkflowInstances` support soft delete. A soft-deleted record is invisible to all standard queries via the Global Query Filter. Hard deletion is never performed; this ensures audit trail integrity.

`WorkflowHistory` and `Approvals` are **never deleted** — not even soft deleted. They are the permanent record.

---

## 6. Audit Columns

Every mutable table includes these four audit columns:

| Column | Type | Purpose |
|--------|------|---------|
| `CreatedAt` | `timestamptz` | When the record was first created |
| `CreatedBy` | `varchar(200)` | Actor ID who created it |
| `ModifiedAt` | `timestamptz` | When the record was last modified |
| `ModifiedBy` | `varchar(200)` | Actor ID who last modified it |

These are set automatically by EF Core `SaveChangesInterceptor` in the Infrastructure layer. No command handler or service needs to set them manually.

---

## 7. Migration Strategy

Arora.Workflow ships its own EF Core migrations. The host application runs them as part of its migration bundle. The migration assembly is `Arora.Workflow.Infrastructure`.

```csharp
// In host application startup:
await app.Services.ApplyAroraWorkflowMigrationsAsync();
```

All Arora.Workflow tables are prefixed with `aw_` to avoid naming conflicts with host application tables:
- `aw_workflow_definitions`
- `aw_workflow_step_definitions`
- `aw_escalation_policies`
- `aw_workflow_instances`
- `aw_step_results`
- `aw_approvals`
- `aw_workflow_history`
- `aw_workflow_deadlines`
