# Benchmark & Performance Design Targets
# Arora.Workflow

**Version**: 1.0
**Status**: Draft — Aspirational (Phase 1)
**Date**: 2026-07-01

---

> *Phase 1 benchmarks are design targets, not measured results.*
> *Empirical benchmarks using BenchmarkDotNet are produced in Phase 2 when working code exists.*
> *If an implementation cannot meet these targets, it is redesigned — not the targets.*

---

## 1. Performance Philosophy

Arora.Workflow targets human-pace business approval workflows. A workflow instance transitions when a human makes a decision — which occurs over minutes, hours, or days, not milliseconds. This context shapes all performance targets:

- **Throughput** is not the primary constraint. An enterprise invoice approval system might process 500 invoices per day.
- **Latency** matters for the synchronous parts of the API: starting a workflow, submitting an approval decision, querying pending approvals.
- **Scalability** means "100 concurrent waiting workflow instances on commodity hardware" — not 100,000 events per second.
- **Reliability** outranks performance. A correct result 10ms late is better than an incorrect result immediately.

---

## 2. Latency Targets (p99, single database host)

| Operation | Target (p99) | Notes |
|-----------|-------------|-------|
| `IWorkflowService.StartAsync()` | < 50ms | Includes: definition load (cached), instance create, first step execute, history write |
| `IApprovalService.ApproveAsync()` | < 30ms | Includes: approval record update, state transition, history write, event dispatch |
| `IApprovalService.GetPendingApprovalsAsync()` | < 20ms | Single indexed query on `(TenantId, AssignedActorId, Status)` |
| `IWorkflowService.GetHistoryAsync()` | < 30ms | Ordered index scan on `(WorkflowInstanceId, OccurredAt)` |
| `IWorkflowService.GetInstanceAsync()` | < 10ms | Primary key lookup |
| Escalation scheduler poll cycle | < 100ms | Processes up to 100 elapsed deadlines per cycle |

---

## 3. Concurrency Target

**100 concurrent workflow instances without performance degradation.**

This means:
- 100 `WorkflowInstance` rows in `PendingApproval` state simultaneously
- Approval decisions submitted concurrently by different actors on different instances
- Optimistic concurrency (`RowVersion`) resolves concurrent modifications to the same instance
- No global locks; instance-level locking only

---

## 4. Database Query Budget

Every database interaction in the hot path must be designed upfront. No query may be added to the hot path without a corresponding index.

### Hot Queries

| Query | Table | Index |
|-------|-------|-------|
| Load workflow definition by name + version | `aw_workflow_definitions` | `(TenantId, Name, Version)` |
| Load workflow instance by ID | `aw_workflow_instances` | `PRIMARY KEY (Id)` |
| Check idempotency key on start | `aw_workflow_instances` | `UNIQUE (TenantId, IdempotencyKey)` |
| Get pending approvals for actor | `aw_approvals` | `(TenantId, AssignedActorId, Status)` |
| Get instance history | `aw_workflow_history` | `(WorkflowInstanceId, OccurredAt)` |
| Escalation poller query | `aw_workflow_deadlines` | `(IsProcessed, FireAt)` |

### N+1 Prevention

All standard `GetInstanceAsync()` calls load step results and approvals in a single query using explicit `.Include()`. No lazy loading is enabled.

```csharp
// Correct — single query with includes
var instance = await _context.WorkflowInstances
    .Include(x => x.StepResults)
    .Include(x => x.Approvals)
    .FirstOrDefaultAsync(x => x.Id == instanceId, ct);

// Forbidden — N+1 via lazy loading
var instance = await _context.WorkflowInstances.FindAsync(instanceId);
var approvals = instance.Approvals; // lazy load = additional query per instance
```

---

## 5. Caching Strategy

### Workflow Definition Cache

`WorkflowDefinition` objects are immutable once published. They are loaded from the database on first use and cached in memory per definition name + version.

Cache implementation: `IMemoryCache` (built into ASP.NET Core). No distributed cache required for Phase 1.

Cache invalidation: When a definition is deprecated or a new version is published, the cache entry for that name is evicted. Cache TTL: 60 minutes as a safety net.

**Why this matters for latency**: Without caching, every `StartAsync()` call requires a join between `WorkflowDefinitions` and `WorkflowStepDefinitions`. With caching, the definition is in memory — the only required query is the instance write.

---

## 6. Empirical Benchmark Plan (Phase 2)

Once working code exists, BenchmarkDotNet benchmarks are added to `benchmarks/Arora.Workflow.Benchmarks/`.

### Benchmark Scenarios

| Scenario | Measured Metric |
|----------|----------------|
| `StartAsync` — cold (no cache) | p50, p95, p99 latency |
| `StartAsync` — warm (cached definition) | p50, p95, p99 latency |
| `ApproveAsync` — single approver | p50, p95, p99 latency |
| `GetPendingApprovalsAsync` — 100 pending items | p50, p95, p99 latency |
| `GetHistoryAsync` — 50 history entries | p50, p95, p99 latency |
| Escalation scheduler — 100 expired deadlines | Total processing time |
| Concurrent `ApproveAsync` — 10 concurrent on same instance | Conflict resolution rate, latency |

### Benchmark Infrastructure

- PostgreSQL via `Testcontainers.PostgreSql` (same version as production)
- `BenchmarkDotNet` with `[MemoryDiagnoser]` attribute
- Benchmarks run in CI on release branches (not on every PR — too slow)
- Results stored in `benchmarks/results/` and committed to version control

### Regression Policy

If a benchmark result regresses by > 20% compared to the previous release:
1. The regression is filed as a P1 issue.
2. The release is blocked until the regression is resolved or explicitly accepted with documented justification.

---

## 7. What We Are Not Optimizing For

| Scenario | Why Not Targeted |
|----------|-----------------|
| > 1,000 workflow starts/second | Not the target use case (see `ProductStrategy.md`) |
| Sub-millisecond step execution | Steps perform I/O; sub-millisecond is not achievable or required |
| In-memory-only execution | Violates Principle 1 (Database is Truth) |
| Zero allocation hot path | Acceptable allocations for the correctness guarantees provided |
