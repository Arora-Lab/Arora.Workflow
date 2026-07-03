# ADR-001 — State Machine vs. Flowchart / BPMN Model
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

A workflow engine must choose a fundamental execution model. The two dominant models in the industry are:

1. **Flowchart / BPMN model**: Nodes are activities; edges are control flow. The engine "walks" the graph from start to end. Used by Elsa, Camunda, Flowable, jBPM.
2. **State machine model**: States are positions; transitions are the only way to move between them. The engine only moves when a trigger fires and a guard passes. Used by Stateless, Automatonymous (MassTransit), XState.

---

## Options Considered

| Model | Pros | Cons |
|-------|------|------|
| BPMN / Flowchart | Industry standard, visual tooling, rich notation | Complex to implement, heavyweight to learn, poor fit for code-first definition |
| State machine | Simple, deterministic, testable, code-first friendly | No standard notation, requires discipline in definition design |
| Hybrid | Best of both worlds | Highest implementation complexity; hard to reason about |

---

## Decision

**State machine model.**

---

## Why

1. **Determinism**: State machines are deterministic by design. Given an input and a trigger, exactly one transition is valid. This is Principle 2 of the architecture.
2. **Testability**: State machines are trivially testable. Apply a trigger; assert the new state. No graph traversal, no mock engine.
3. **Code-first API**: State machines map directly to the fluent builder API. `WithApproval("step").OnApprove(next: "x").OnReject(next: "y")` is natural; BPMN gateways are not.
4. **Observability**: The current state is always a single named value in the database. "Where is this workflow?" has a one-field answer.
5. **Scope alignment**: Arora.Workflow targets business approval workflows — a problem domain where the set of states is known, finite, and business-meaningful (PendingManagerApproval, PendingFinanceApproval, Approved, Rejected). State machines are a perfect fit.

---

## Trade-offs

- Cannot import/export BPMN. This is a deliberate limitation documented in `ProductStrategy.md`.
- Visual designer requires a custom graph renderer, not an off-the-shelf BPMN tool. This is acceptable — the designer is Phase 5.
- Parallel execution requires explicit `WithParallelGroup` syntax rather than BPMN fork/join notation. The explicit syntax is clearer in code.

---

## Consequences

- `WorkflowDefinition` is a state machine definition: a set of named states and a set of transitions between them.
- Every transition has a trigger type (StepCompleted, ApprovalGranted, ApprovalRejected, EscalationFired) and an optional guard.
- The `TransitionEvaluator` enforces that exactly one transition matches any given trigger+guard combination.
- `CurrentState` on `WorkflowInstance` is always a single named string value in the database.
