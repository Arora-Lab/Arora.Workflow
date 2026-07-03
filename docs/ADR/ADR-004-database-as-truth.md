# ADR-004 — Database as Truth (No In-Memory-Only State)
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

Some workflow engines maintain workflow state primarily in memory and rely on event replay (event sourcing) or periodic snapshots to reconstruct state after a restart. This is a common pattern in systems like Temporal and Akka.

Arora.Workflow must choose a durability model.

---

## Options Considered

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| Database as primary truth | Every transition writes to DB before completing | Zero data loss on crash, queryable, auditable | Higher write latency per transition |
| In-memory with event sourcing | State reconstructed from event log on restart | Fast execution, natural event history | Complex reconstruction, replay latency on restart |
| In-memory with periodic DB snapshots | State snapshotted every N transitions | Fast execution between snapshots | Up to N transitions lost on crash |

---

## Decision

**Database as the single, authoritative source of truth. No in-memory-only state.**

This is Principle 1 of the architecture (`Principles.md`).

---

## Why

1. **Correctness at the target scale**: Business approval workflows are low-frequency, human-pace operations. A transition occurs when a manager approves — not thousands of times per second. The write overhead of persisting before completing is irrelevant at this scale.
2. **Crash resilience by default**: If the process crashes mid-execution, the next execution resumes from the persisted state. No replay needed. No state loss. This is the behavior developers expect and do not have to think about.
3. **Free audit trail**: Principle 5 (No Hidden State) requires that every transition is observable. When the database is truth, the history is already there. Event sourcing requires an explicit projection step.
4. **SQL is the query language**: Business applications already query SQL. Finding "all invoices in PendingFinanceApproval state" is a single SQL query when `CurrentState` lives in the database. With event sourcing, it requires a projection or a full replay.
5. **Operational simplicity**: Event sourcing adds operational complexity: snapshot compaction, replay performance, projection management. Arora.Workflow's target user is building a business application, not managing a distributed systems infrastructure.

---

## Trade-offs

- Per-transition database writes add latency (typically 5–20ms on a local database). For human-pace approval workflows, this is imperceptible.
- High-throughput automated workflows (thousands of transitions per second) are not a fit for Arora.Workflow. This is documented in `ProductStrategy.md` as an explicit out-of-scope use case.

---

## Consequences

- Every call to the workflow engine that changes state results in at least one database write before the method returns.
- `WorkflowHistory` is written within the same transaction as the state transition.
- There is no "eventually consistent" state model. State is either committed or the transition did not happen.
- The escalation scheduler reads `WorkflowDeadlines` from the database — it does not maintain a memory queue of pending timers.
