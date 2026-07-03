# Code Map
# Arora.Workflow

**Version**: 1.3 (updated as implementation progresses)
**Last Updated**: Phase 2 — Domain + Application + Infrastructure complete

> *This document is the authoritative map of the Arora.Workflow source tree.*
> *Every file is listed with a one-line description of its purpose.*
> *Update this document whenever a new file is added.*

---

## Project Structure

```
Arora Workflow/
├── docs/                          ← Phase 1 documentation
├── src/
│   ├── Arora.Workflow.slnx        ← solution container (all projects)
│   ├── Arora.Workflow/            ← core engine (no EF Core dependency)
│   ├── Arora.Workflow.EntityFramework/  ← EF Core persistence (separate NuGet)
│   └── Arora.Workflow.Testing/    ← test helpers (never ships to production)
└── tests/
    ├── Arora.Workflow.UnitTests/
    └── Arora.Workflow.IntegrationTests/
```

---

## Project Dependency Graph

```
Arora.Workflow                  (no internal dependencies)
  ↑
  ├── Arora.Workflow.EntityFramework
  ├── Arora.Workflow.Testing
  └── Arora.Workflow.UnitTests (also → Testing)
        ↑
  Arora.Workflow.IntegrationTests (Core + EF + Testing)
```

**Rule**: Core never references EntityFramework. That's ADR-007.

---

## NuGet Package Dependencies

| Project | Packages |
|---------|---------|
| `Arora.Workflow` | `MediatR 12.x`, `Microsoft.Extensions.Logging.Abstractions` |
| `Arora.Workflow.EntityFramework` | `Microsoft.EntityFrameworkCore 9.x` |
| `Arora.Workflow.Testing` | *(none yet)* |
| `Arora.Workflow.UnitTests` | xUnit (auto) |
| `Arora.Workflow.IntegrationTests` | xUnit (auto) |

---

## `src/Arora.Workflow` — Core Library

### `Domain/ValueObjects/`

Value objects: defined entirely by their properties, no database identity. All are `sealed record` or `enum`.

| File | Type | Purpose |
|------|------|---------|
| `ActorInfo.cs` | `sealed record` | Who performed an action (Id + DisplayName). Includes `ActorInfo.System` for automated engine actions. |
| `WorkflowState.cs` | `sealed record` | A named position in the lifecycle. Carries `Name` + `StateType`. Computed properties: `IsTerminal`, `RequiresApproval`. |
| `WorkflowStateType.cs` | `enum` | Classifies a state's role: `Initial`, `Intermediate`, `PendingApproval`, `Completed`, `Rejected`, `Cancelled`. |
| `WorkflowStatus.cs` | `enum` | The simplified external-facing status: `Running`, `PendingApproval`, `Completed`, `Rejected`, `Cancelled`. Distinct from `WorkflowState` — this is what the UI and APIs expose. |
| `WorkflowDefinitionStatus.cs` | `enum` | Lifecycle stage of a definition: `Draft`, `Published`, `Deprecated`. |
| `RetryPolicy.cs` | `sealed record` | Step retry configuration: `MaxAttempts`, `InitialDelay`, `Backoff`. Contains `GetDelay(int attemptNumber)` which computes the wait time. Includes `RetryPolicy.Default` and `RetryPolicy.None` presets. |
| `BackoffStrategy.cs` | `enum` | Controls delay growth between retries: `Fixed` (same delay), `Linear` (delay × attempt), `Exponential` (delay × 2^attempt). |

---

### `Domain/Events/`

Domain events: immutable facts published by the engine after every state change. All are `sealed record` implementing `IWorkflowEvent : INotification`.

The engine never sends emails or calls APIs directly. It publishes events. Plugins handle the rest.

| File | Event(s) | When Raised |
|------|---------|------------|
| `IWorkflowEvent.cs` | `IWorkflowEvent` | Marker interface. Extends MediatR `INotification`. Every event has `WorkflowInstanceId` and `OccurredAt`. |
| `WorkflowStarted.cs` | `WorkflowStarted` | A new `WorkflowInstance` was created and execution has begun. |
| `WorkflowTransitioned.cs` | `WorkflowTransitioned` | The state machine moved from one state to another. Raised on every transition — the most common event. |
| `StepExecuted.cs` | `StepExecuted` | A step completed successfully (includes attempt number and duration). |
| `StepFailed.cs` | `StepFailed` | A step exhausted all retries and failed permanently. |
| `ApprovalRequested.cs` | `ApprovalRequested` | An approval record was created. **Primary trigger for notification plugins** (Teams, Slack, email). |
| `ApprovalDecisions.cs` | `ApprovalGranted`, `ApprovalRejected` | An actor submitted an approve or reject decision. |
| `WorkflowEscalated.cs` | `WorkflowEscalated` | A deadline elapsed and the approval was re-routed to a higher-authority actor. |
| `WorkflowTerminalEvents.cs` | `WorkflowCompleted`, `WorkflowRejected`, `WorkflowCancelled` | The three ways a workflow can end. |

---

### `Domain/Exceptions/`

All exceptions inherit `WorkflowException`, which carries a machine-readable `ErrorCode` alongside the human-readable `Message`.

```
WorkflowException (abstract base — has ErrorCode property)
  │
  ├── NotFoundExceptions.cs
  │     ├── WorkflowNotFoundException              WORKFLOW_NOT_FOUND
  │     └── WorkflowDefinitionNotFoundException    DEFINITION_NOT_FOUND
  │
  ├── TransitionExceptions.cs
  │     ├── InvalidTransitionException             INVALID_TRANSITION
  │     └── AmbiguousTransitionException           AMBIGUOUS_TRANSITION
  │
  ├── StateExceptions.cs
  │     ├── WorkflowInTerminalStateException       TERMINAL_STATE
  │     ├── WorkflowAlreadyExistsException         WORKFLOW_ALREADY_EXISTS
  │     └── DuplicateApprovalException             DUPLICATE_APPROVAL
  │
  └── StepExecutionException.cs
        └── StepExecutionException                 STEP_EXECUTION_FAILED
```

| File | Exception(s) | When Thrown |
|------|-------------|------------|
| `WorkflowException.cs` | `WorkflowException` | Abstract base. Catch this to handle any Arora.Workflow error. |
| `NotFoundExceptions.cs` | `WorkflowNotFoundException` | No instance found for the given ID. |
| | `WorkflowDefinitionNotFoundException` | No published definition found for name/version. |
| `TransitionExceptions.cs` | `InvalidTransitionException` | No valid transition from current state for the given trigger. |
| | `AmbiguousTransitionException` | More than one transition matched — authoring error, guards must be mutually exclusive. |
| `StateExceptions.cs` | `WorkflowInTerminalStateException` | Tried to mutate an instance already in Completed/Rejected/Cancelled. |
| | `WorkflowAlreadyExistsException` | Idempotency key already used for another instance. |
| | `DuplicateApprovalException` | Approval already decided; cannot submit another decision. |
| `StepExecutionException.cs` | `StepExecutionException` | Step threw an exception after exhausting all retries. Wraps the original exception as `InnerException`. |

---

### `Domain/Aggregates/`

Aggregate roots: the consistency boundaries of the domain. All state changes go through these classes. They enforce invariants and collect domain events.

**Key patterns used:**
- **Private constructor + static factory method** — prevents creating invalid aggregates via `new`
- **Domain event collection** — events are held in `_domainEvents`, published by the engine *after* `SaveChanges()`, never from within the aggregate
- **Single enforcement point** — each invariant check appears in exactly one method

| File | Class | Purpose |
|------|-------|---------|
| `WorkflowInstance.cs` | `WorkflowInstance` | **The heart of the system.** Tracks current state, enforces terminal-state invariant, collects events. Key methods: `Start()` (factory), `TransitionTo()`, `Cancel()`. Key queries: `IsInTerminalState()`, `IsPendingApproval()`. |
| `WorkflowDefinition.cs` | `WorkflowDefinition` | The blueprint. Enforces `Draft → Published → Deprecated` lifecycle. Key methods: `Create()` (factory), `Publish()`, `Deprecate()`, `CreateNewVersion()`. Immutable once Published. |

---

## `src/Arora.Workflow` — Application Layer

### `Application/Interfaces/`

Contracts between the engine and the outside world. Host applications inject these services; EF Core implements the repositories.

| File | Type(s) | Purpose |
|------|---------|--------|
| `StartWorkflowRequest.cs` | `sealed record` | Input DTO for `IWorkflowService.StartAsync`. Uses C# `required` properties for compile-time safety. |
| `ResponseDtos.cs` | `sealed record` ×3 | `WorkflowInstanceSnapshot` (current state), `WorkflowHistoryEntry` (audit), `PendingApproval` (approval queue). Pure read models — no domain methods. |
| `IWorkflowService.cs` | `interface` | Primary service: `StartAsync`, `GetInstanceAsync`, `GetByCorrelationIdAsync`, `GetHistoryAsync`, `CancelAsync`. |
| `IApprovalService.cs` | `interface` | Approval service: `GetPendingApprovalsAsync`, `GetApprovalAsync`, `ApproveAsync`, `RejectAsync`. |
| `IRepositories.cs` | `interface` ×2 | `IWorkflowDefinitionRepository` + `IWorkflowInstanceRepository`. Persistence contracts implemented by the EntityFramework project. |

### `Application/Services/`

Concrete implementations of the public service interfaces. `internal sealed` classes — not directly accessible to host applications.

| File | Class | Purpose |
|------|-------|---------|
| `WorkflowMapper.cs` | `WorkflowMapper` | Static projections: aggregate → DTO. No dependencies. |
| `WorkflowService.cs` | `WorkflowService` | Implements `IWorkflowService`. 8-step `StartAsync` orchestrates: idempotency check → definition lookup → aggregate creation → persist → publish events → engine advance → persist → publish events. |
| `ApprovalService.cs` | `ApprovalService` | Implements `IApprovalService`. Decision flow: load approval → guard duplicate → load instance → engine advance → persist → publish. |

**Key pattern in `StartAsync`**: Persist *before* calling the engine. If the engine throws and is retried, the idempotency check prevents a duplicate instance.

**Key pattern**: Events are published *after* `SaveChangesAsync()`. If publishing fails, the state is safely on disk.

---

## `src/Arora.Workflow.EntityFramework` — EF Core Integration

*(Implementation pending — Phase 2)*

Will contain:
- EF Core entity configurations for all 8 `aw_*` tables
- Repository implementations
- `AroraWorkflowDbContext` base class
- EF Core migrations

---

## `src/Arora.Workflow.Testing` — Test Helpers

*(Implementation pending — Phase 2)*

Will contain:
- `TestWorkflowHost` — sets up the engine in-memory for unit tests
- `WorkflowInstanceBuilder` — fluent builder for test data
- Fake clock and tenant context implementations

---

## `tests/Arora.Workflow.UnitTests`

*(Implementation pending — Phase 2)*

Target coverage: Domain ≥ 90%, Application ≥ 80%

---

## `tests/Arora.Workflow.IntegrationTests`

*(Implementation pending — Phase 2)*

Uses `Testcontainers.PostgreSql` for real database behavior.

---

## Status by Layer

| Layer | Status | Files Complete |
|-------|--------|---------------|
| Domain / Value Objects | ✅ Complete | 7 |
| Domain / Events | ✅ Complete | 9 |
| Domain / Exceptions | ✅ Complete | 5 |
| Domain / Aggregates | ✅ Complete | 2 |
| Application / Interfaces | ✅ Complete | 8 |
| Application / Services | ✅ Complete | 3 |
| Infrastructure / EF Core | ✅ Complete | 8 |
| Infrastructure / Repositories | ✅ Complete | 2 |
| Infrastructure / Engine | ⚠️ Stub | 1 |
| DI Registration | ✅ Complete | 3 |
| Testing Helpers | ⬜ Next | — |
| Unit Tests | ⬜ Pending | — |
| Integration Tests | ⬜ Pending | — |
