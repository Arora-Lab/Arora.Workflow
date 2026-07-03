# Domain-Driven Design Model
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01

---

> *This is the authoritative glossary of the Arora.Workflow domain.*
> *Every document in this repository uses these terms exactly as defined here.*
> *If a term is not in this document, it does not exist in the Arora.Workflow domain.*

---

## 1. Bounded Context

Arora.Workflow owns **workflow orchestration**.

It does not own the business entities that workflows operate on. An invoice, a purchase order, a vendor, a leave request — these belong to the host application's domain. Arora.Workflow references them only by a correlation ID (`CorrelationId: string`), which the host application provides when starting a workflow instance.

**What Arora.Workflow owns:**
- Workflow definitions (the structure and rules)
- Workflow instances (the runtime execution state)
- Step execution records and results
- Approval decisions
- Escalation policies and timers
- Workflow history (complete audit trail)

**What Arora.Workflow does NOT own:**
- The business entity being approved (invoice, PO, etc.)
- The identity and permissions system (it consumes actor identity; it does not define it)
- The notification delivery mechanism (it raises events; it does not send emails)
- The UI for making approval decisions (it provides APIs; it does not provide screens)

---

## 2. Ubiquitous Language

These terms have precise definitions in the Arora.Workflow domain. They are used consistently in code, documentation, and conversation.

### Workflow Definition

A **Workflow Definition** is the blueprint for a category of workflow. It describes the steps, transitions, approval requirements, escalation policies, and guards that apply to all instances of that workflow. A Workflow Definition is immutable once published — changes require a new version.

*Example: "Invoice Approval v2" is a Workflow Definition.*

### Workflow Instance

A **Workflow Instance** is a single execution of a Workflow Definition, associated with a specific business entity via a `CorrelationId`. An instance has a current `State`, a history of transitions, and zero or more pending approvals at any given time.

*Example: "Invoice #INV-2024-0047 going through Invoice Approval v2" is a Workflow Instance.*

### Step

A **Step** is a unit of work within a Workflow Definition. A step has a name, an input type, an output type, a retry policy, and an idempotency contract. Steps are authored by developers implementing `IWorkflowStep<TInput, TOutput>`. The engine calls steps; steps do not call the engine.

*Examples: `ValidateInvoiceStep`, `ProcessPaymentStep`, `SendRejectionNotificationStep`.*

### Approval Step

An **Approval Step** is a special kind of step that pauses execution and waits for a human actor to make a decision. An approval step defines who can approve, what constitutes approval vs. rejection, and what happens in each case. An approval step may define an escalation policy that fires if no decision is made within a deadline.

*Example: The "Manager Approval" step in an invoice workflow.*

### State

A **State** is a named position in the lifecycle of a Workflow Instance. States are defined by the Workflow Definition. At any moment, an instance is in exactly one state.

*Examples: `Draft`, `PendingManagerApproval`, `PendingFinanceApproval`, `Approved`, `Rejected`, `Cancelled`.*

### Transition

A **Transition** is a directed edge from one State to another, triggered by an Event and optionally guarded by a `TransitionGuard`. The engine evaluates all valid transitions from the current state when an event is received. If a guard is defined, it must evaluate to `true` for the transition to proceed.

*Example: The transition from `PendingManagerApproval` to `PendingFinanceApproval`, triggered by the `ApprovalGranted` event, guarded by `invoice.Amount > 10000`.*

### Trigger

A **Trigger** is the mechanism that initiates a transition. Triggers include: approval decisions (approve/reject), time-based escalations (deadline elapsed), external signals (a step completing), and manual interventions (cancellation).

### Actor

An **Actor** is the identity of the person or system that performs an action on a Workflow Instance. Actors are provided by the host application. Arora.Workflow stores the actor's ID and display name but does not manage actor identity or permissions — that is the host application's responsibility.

*Example: `{ ActorId: "usr_12345", ActorName: "Jane Smith" }`*

### Approval

An **Approval** is the record of a decision made by an Actor on an Approval Step. An Approval has a status (`Pending`, `Approved`, `Rejected`, `Escalated`, `Withdrawn`), an actor, a timestamp, and an optional comment.

### Escalation

An **Escalation** is the automatic promotion of a pending Approval to a higher-authority actor when a deadline elapses without a decision. An Escalation is defined on an Approval Step as an `EscalationPolicy`, which specifies the deadline duration and the escalation target (an actor ID or a role).

### History Entry

A **History Entry** is an immutable record of a single event that occurred on a Workflow Instance. Every state transition, step execution, approval decision, escalation, and cancellation produces a History Entry. The collection of all History Entries for an instance is its **Workflow History** — the complete, ordered audit trail.

### Deadline

A **Deadline** is a time-bound constraint attached to a Workflow Instance or to an Approval Step. When a deadline elapses without the expected event occurring, the engine fires an `EscalationTimerElapsed` domain event. The escalation handler processes this event and takes the configured action (escalating the approver, auto-rejecting, or cancelling the instance).

### Workflow Definition Version

A **Version** is an integer assigned to each published Workflow Definition. When a Workflow Definition changes, a new version is published. Existing instances continue to execute against the version they were started with. The engine supports running multiple versions of the same Workflow Definition simultaneously.

---

## 3. Aggregate Roots

Aggregate roots are the consistency boundaries of the domain. All state changes to an aggregate go through the aggregate root. The aggregate enforces its own invariants.

### WorkflowDefinition

The aggregate root for workflow structure.

**Identity:** `WorkflowDefinitionId` (GUID)

**Invariants:**
- A Workflow Definition must have at least one step.
- Every step referenced in a transition must exist in the definition.
- A Workflow Definition must have exactly one terminal state of type `Completed` and may have one of type `Cancelled` and one of type `Rejected`.
- A Workflow Definition may only be mutated in `Draft` status. Once `Published`, it is immutable. Changes require creating a new version.
- Version numbers are monotonically increasing integers. They may not be reused.

**Lifecycle states:** `Draft` → `Published` → `Deprecated`

**Key behaviors:**
- `Publish()` — validates all invariants and transitions the definition to `Published`
- `Deprecate()` — prevents new instances from being started; does not affect running instances
- `CreateNewVersion()` — creates a new `Draft` version based on this definition

### WorkflowInstance

The aggregate root for workflow execution.

**Identity:** `WorkflowInstanceId` (GUID)

**Invariants:**
- A Workflow Instance is always associated with a `WorkflowDefinition` by `WorkflowDefinitionId` and `WorkflowDefinitionVersion`.
- A Workflow Instance always has a current `State`.
- A Workflow Instance in a terminal state (`Completed`, `Rejected`, `Cancelled`) may not transition to any other state.
- A Workflow Instance may have at most one pending Approval per Approval Step at any time.
- The `CorrelationId` is unique across all instances of the same `WorkflowDefinitionId`. (The same business entity cannot have two active instances of the same workflow type.)

**Lifecycle states:** `NotStarted` → `Running` → `[PendingApproval]` → `Completed` | `Rejected` | `Cancelled`

**Key behaviors:**
- `Start(input, actor, idempotencyKey)` — validates preconditions and emits `WorkflowStarted`
- `TransitionTo(toState, trigger, actor)` — validates the transition is permitted and emits `WorkflowTransitioned`
- `Cancel(reason, actor)` — transitions to `Cancelled` regardless of current state (unless already terminal)
- `AddHistoryEntry(entry)` — appends an immutable history entry; cannot be undone

---

## 4. Entities

Entities have identity and lifecycle, but they are not aggregate roots. They are accessed through their parent aggregate root.

### WorkflowStep

A step definition within a `WorkflowDefinition`. Defines the step's name, the implementation type (`IWorkflowStep<TInput, TOutput>`), the input schema, the retry policy, and whether it is an approval step.

**Identity:** `WorkflowStepId` (GUID)
**Parent aggregate:** `WorkflowDefinition`

### StepResult

The recorded outcome of a single execution of a `WorkflowStep` within a `WorkflowInstance`. Contains the output of the step, the duration, the success/failure status, and any error detail. Multiple `StepResult` records may exist for the same step if the step was retried.

**Identity:** `StepResultId` (GUID)
**Parent aggregate:** `WorkflowInstance`

### Approval

The record of a pending or decided approval request on an Approval Step within a `WorkflowInstance`. An Approval is `Pending` until the actor submits a decision.

**Identity:** `ApprovalId` (GUID)
**Parent aggregate:** `WorkflowInstance`

### EscalationPolicy

The configuration for automatic escalation on an Approval Step. Defines the deadline duration and the escalation target. Attached to a `WorkflowStep` of type `Approval`.

**Identity:** `EscalationPolicyId` (GUID)
**Parent aggregate:** `WorkflowDefinition`

---

## 5. Value Objects

Value objects have no identity. They are defined entirely by their properties. Two value objects with identical properties are equal.

### WorkflowState

Represents a named state in a Workflow Definition. Carries the state `Name` (string) and `StateType` (enum: `Initial`, `Intermediate`, `PendingApproval`, `Completed`, `Rejected`, `Cancelled`).

```csharp
public record WorkflowState(string Name, WorkflowStateType StateType);
```

### TransitionGuard

A predicate expression that must evaluate to `true` for a transition to be eligible. Expressed as a C# delegate over the workflow input and current context.

```csharp
public record TransitionGuard(string Description, Func<WorkflowContext, bool> Predicate);
```

### RetryPolicy

The retry configuration for a step. Defines the maximum attempt count, the delay between retries, and the backoff strategy (fixed, linear, exponential).

```csharp
public record RetryPolicy(int MaxAttempts, TimeSpan InitialDelay, BackoffStrategy Backoff);
```

### DeadlineSpec

The deadline configuration for an escalation. Defines the duration after which the escalation fires, relative to when the Approval was created.

```csharp
public record DeadlineSpec(TimeSpan Duration, DateTimeOffset? AbsoluteDeadline = null);
```

---

## 6. Domain Events

Domain events represent facts that have occurred within the Arora.Workflow domain. They are immutable records raised by aggregate roots after state changes are committed.

Events are dispatched in-process via MediatR `INotification` in Phase 1. In Phase 2, they may be promoted to a durable message broker (Azure Service Bus, RabbitMQ) without changing the event schema or handler logic.

| Event | Raised When |
|-------|-------------|
| `WorkflowStarted` | A new `WorkflowInstance` is created and started |
| `WorkflowTransitioned` | A `WorkflowInstance` moves from one state to another |
| `StepExecuted` | A step completes successfully |
| `StepFailed` | A step exhausts its retry policy and fails |
| `ApprovalRequested` | An Approval is created on an Approval Step (notifies the approver) |
| `ApprovalGranted` | An Actor approves a pending Approval |
| `ApprovalRejected` | An Actor rejects a pending Approval |
| `ApprovalWithdrawn` | A previously submitted Approval decision is withdrawn (if permitted) |
| `EscalationTimerElapsed` | A deadline elapses without an Approval decision |
| `WorkflowEscalated` | An Approval is escalated to a higher-authority actor |
| `WorkflowCompleted` | A `WorkflowInstance` reaches a `Completed` terminal state |
| `WorkflowRejected` | A `WorkflowInstance` reaches a `Rejected` terminal state |
| `WorkflowCancelled` | A `WorkflowInstance` is cancelled |

---

## 7. Domain Services

Domain services encapsulate logic that does not naturally belong to a single aggregate.

### WorkflowEngine

The central orchestration service. Accepts triggers (start, approve, reject, cancel, signal), evaluates the current state of a `WorkflowInstance`, determines the valid transitions, executes steps, and persists results. The engine is the only entry point for mutating a `WorkflowInstance`.

### WorkflowDefinitionValidator

Validates a `WorkflowDefinition` in `Draft` status against all invariants before publication. Returns a structured `ValidationResult` with the specific violations found.

### TransitionEvaluator

Evaluates the set of valid transitions from a given state, applying all `TransitionGuard` predicates to the current `WorkflowContext`. Returns the single eligible transition (or an error if zero or more than one match).

### EscalationScheduler

Registers and cancels deadline timers for Approval Steps. In Phase 1, implemented using a background `IHostedService`. In Phase 2, may be promoted to a durable scheduled message.

---

## 8. Repositories

Repositories are infrastructure-layer abstractions defined in the Application layer. They are implemented with EF Core in the Infrastructure layer.

```csharp
public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(WorkflowDefinitionId id, CancellationToken ct);
    Task<WorkflowDefinition?> GetByNameAndVersionAsync(string name, int version, CancellationToken ct);
    Task AddAsync(WorkflowDefinition definition, CancellationToken ct);
    Task UpdateAsync(WorkflowDefinition definition, CancellationToken ct);
}

public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(WorkflowInstanceId id, CancellationToken ct);
    Task<WorkflowInstance?> GetByCorrelationIdAsync(string correlationId, WorkflowDefinitionId definitionId, CancellationToken ct);
    Task AddAsync(WorkflowInstance instance, CancellationToken ct);
    Task UpdateAsync(WorkflowInstance instance, CancellationToken ct);
    Task<IReadOnlyList<WorkflowHistory>> GetHistoryAsync(WorkflowInstanceId id, CancellationToken ct);
}
```
