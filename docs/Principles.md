# Architecture Principles
# Arora.Workflow

**Version**: 1.0
**Status**: Accepted
**Date**: 2026-07-01

---

> *These five principles are the laws of Arora.Workflow.*
> *Every architectural decision is evaluated against them.*
> *An implementation that violates a principle is wrong, regardless of how clever it is.*
> *When a principle must be broken, an ADR is written and approved first.*

---

## Principle 1 — Database Is Truth

**State lives in the database. Always.**

A workflow instance has one authoritative representation: the row in the `WorkflowInstances` table. Not a variable in memory. Not a cache entry. Not a message in a queue. The database row.

**Why this matters:**

The moment you allow in-memory-only state, you have a distributed systems problem disguised as an application problem. If the process crashes between two steps, what is the current state of the workflow? If you cached the state, you do not know. If the database is the truth, the answer is always one query away.

**Consequences:**

- Every state transition is persisted *before* the transition is considered complete. There is no "confirm the write later" pattern.
- Reads of workflow state always go to the database (with caching as an optimization layer, never as a replacement).
- The `WorkflowHistory` table is written synchronously with each transition, not asynchronously.
- If the database write fails, the transition does not complete. The engine retries.

**What this rules out:**

- In-memory workflow engines where state reconstruction requires replaying events
- Caching workflow state without a write-through strategy
- Any design where a process restart changes the observable state of a workflow instance

---

## Principle 2 — Workflow Is Deterministic

**Same inputs. Same definition. Same outcome. Every time.**

A workflow definition is a function. Given a specific set of inputs, a specific sequence of approvals, and a specific workflow definition — the output is always identical. There is no ambient state. There is no randomness in business logic paths. The workflow does not behave differently on a Tuesday.

**Why this matters:**

Determinism is what makes workflows testable. A non-deterministic workflow cannot be reliably unit tested. A workflow that depends on the current time, the calling user's session, or a global static variable is a workflow that will eventually produce an incorrect result that cannot be reproduced.

**Consequences:**

- Workflow definitions are pure descriptions of state transitions. They contain no I/O calls, no database reads, no HTTP calls.
- All inputs to a step are passed explicitly. Steps do not reach into the ambient environment.
- Side effects (sending emails, calling APIs, posting to Teams) happen via domain events dispatched after the state is committed. They are not inline in the step logic.
- Time-dependent behavior (deadlines, escalations) uses injectable clock abstractions, making them testable.

**What this rules out:**

- Steps that read from the database directly to make routing decisions
- Workflow definitions that branch based on `DateTime.Now` without an abstraction
- Global state or static variables inside step implementations

---

## Principle 3 — Everything Asynchronous

**No blocking I/O. No sync-over-async. `async Task` everywhere.**

Every public API method that performs I/O is asynchronous. Every step is `async Task`. Every persistence operation is `async Task`. There are no synchronous overloads, because synchronous I/O is an architectural defect masquerading as a convenience.

**Why this matters:**

Approval workflows involve human-pace decisions. A workflow instance may wait for hours or days for an approver to respond. During that wait, the thread cannot be blocked. The system must scale to hundreds of waiting workflow instances without proportionally scaling thread count.

**Consequences:**

- `IWorkflowStep<TInput, TOutput>` defines `Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken)`. There is no `Execute()` overload.
- `IWorkflowService`, `IApprovalService`, and all public services return `Task<T>`.
- `CancellationToken` is the last parameter on every async public method.
- Synchronous usage (`result.GetAwaiter().GetResult()`) is not supported and produces a warning in analyzer rules.

**What this rules out:**

- Synchronous step execution APIs
- Blocking waits (`Task.Wait()`, `.Result`) anywhere in the engine
- Non-cancellable long-running operations

---

## Principle 4 — Everything Idempotent

**The same operation, applied twice, produces the same result as applying it once.**

Approving an invoice twice does not create two approval records. Retrying a failed step does not charge a customer twice. Reprocessing a `WorkflowStarted` event does not create two workflow instances. The engine enforces idempotency at the persistence layer so that step authors do not have to.

**Why this matters:**

In a distributed system, at-least-once delivery is the honest guarantee. Messages are redelivered. Requests are retried. Network partitions cause partial failures. If your operations are not idempotent, these normal conditions cause silent data corruption. Idempotency is not an optimization — it is a correctness requirement.

**Consequences:**

- Every workflow start operation carries a caller-provided idempotency key. A duplicate start with the same key returns the existing instance instead of creating a new one.
- Every step execution carries a step execution ID. The engine checks for an existing `StepResult` before executing the step.
- Approval operations carry an approval ID. Duplicate approval submissions are detected and rejected with a `DuplicateApprovalException` (not a `500`).
- Step authors are documented in `CodingStandards.md` on the contract: the engine may call `ExecuteAsync` more than once if the first call succeeds but the result is not confirmed.

**What this rules out:**

- Steps that produce side effects on every invocation without checking whether the side effect has already occurred
- Workflow start APIs that create duplicate instances when called twice with the same logical input
- Approval APIs that create duplicate approval records without detection

---

## Principle 5 — No Hidden State

**Every transition is observable. Everything that happens is recorded.**

There is no internal engine state that the host application cannot read. Every approval, rejection, escalation, step execution, failure, retry, and cancellation is recorded in `WorkflowHistory`. The dashboard and audit log are built from this data — not from application logs, not from reconstructed inference.

**Why this matters:**

Audit is not a feature you add later. It is a constraint that shapes the entire data model. An enterprise application without an audit trail is incomplete by definition. When the CFO asks "who approved this invoice and when?" — the answer must come from a database query, not from trawling through log files.

**Consequences:**

- `WorkflowHistory` is written synchronously with every state transition. It is not a background process or a best-effort log.
- Every `WorkflowHistory` entry contains: `WorkflowInstanceId`, `StepName`, `FromState`, `ToState`, `ActorId`, `ActorName`, `Timestamp`, `DurationMs`, and a structured `Metadata` JSON column for step-specific context.
- No "internal" transitions exist. If the engine moves a workflow instance from one state to another for any reason, there is a history entry.
- The `GetHistoryAsync()` API on `IWorkflowService` returns the complete, ordered history for any workflow instance.

**What this rules out:**

- State transitions that are not recorded in `WorkflowHistory`
- Internal engine states that are visible in behavior but not in data
- History tables that are write-optimized without read APIs (the history must be queryable by host applications)

---

## Applying the Principles

These principles are not ideals — they are constraints. When a proposed feature or implementation requires violating a principle, the response is not "let's make an exception." The response is "let's redesign the feature so it does not violate the principle."

When that redesign is genuinely impossible (a real constraint, not a preference), an ADR is written explaining:
1. Which principle is being violated
2. Why the violation is unavoidable
3. What compensating controls are in place
4. What the long-term plan is to restore the principle

The ADR must be approved before the implementation proceeds.
