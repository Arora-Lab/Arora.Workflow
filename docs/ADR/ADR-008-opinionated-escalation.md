# ADR-008 — Escalation as a First-Class Primitive
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

Escalation — automatically re-routing an unanswered approval to a higher authority when a deadline elapses — is a common requirement in business approval workflows. The question is whether escalation should be built into the core engine or implemented as a plugin.

---

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| Plugin: escalation as an external add-on | Smaller core, pluggable | Every team must implement escalation themselves; inconsistent behavior across apps |
| First-class primitive in the core | Consistent, tested, documented; zero configuration to use | Increases core complexity slightly |
| Library of optional patterns | Provides reference implementations | No engine support; teams still write the timer logic |

---

## Decision

**Escalation is a first-class primitive in the `Arora.Workflow` core.** It is defined in the workflow definition API (`.WithEscalation()`), stored in the database (`EscalationPolicies` table, `WorkflowDeadlines` table), and executed by the `EscalationScheduler`.

---

## Why

1. **Universal requirement**: Every business approval workflow that has a deadline needs escalation. This is not an edge case or an advanced feature — it is a baseline requirement for production approval workflows.
2. **The Manifesto**: *"Workflow should be resilient."* An approval workflow with no escalation is a workflow that can silently stall indefinitely. Stalled workflows are a production reliability problem, not an application feature.
3. **Consistent behavior**: If escalation is a plugin, each team implements it differently — different timer mechanisms, different database tables, different behaviors when the escalation actor also fails to respond. The core provides one correct implementation.
4. **Developer experience**: `.WithEscalation(after: TimeSpan.FromDays(2), to: a => a.Role("Manager"))` is two lines. If escalation were a plugin, it would require: installing a separate package, registering additional services, implementing an `IEscalationHandler`, and writing a timer registration call. This violates *"workflow should not require months to learn."*
5. **Database integrity**: Escalation policies must be tied to the database state of the workflow. The `WorkflowDeadlines` table and the `EscalationPolicies` table ensure the escalation fires even if the process restarts.

---

## Trade-offs

- The core package includes the escalation scheduler (`IHostedService`) even for workflows that have no escalation policies. For workflows with no approval steps, the scheduler runs but finds nothing to process. This is negligible overhead.
- Teams that want custom escalation behavior (e.g., "escalate to round-robin from a role") must implement a custom `IEscalationTargetResolver` — which is a supported extension point.

---

## Consequences

- `EscalationPolicies` and `WorkflowDeadlines` tables are part of the core schema.
- `EscalationScheduler` is registered as a background `IHostedService` automatically by `AddAroraWorkflow()`.
- `IApprovalStepBuilder.WithEscalation()` is a public method on the definition builder (Phase 2 SDK).
- Custom escalation target resolution is supported via `IEscalationTargetResolver`, which the host application can implement and register.
