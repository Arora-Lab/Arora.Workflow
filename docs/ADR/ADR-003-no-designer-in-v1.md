# ADR-003 — No Visual Designer in v1
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

Workflow platforms often include a visual drag-and-drop designer for defining workflows. Elsa has one. Camunda has one. Including a designer in v1 would make Arora.Workflow immediately more comparable to these products.

The question is: should the designer be part of Phase 1 or deferred?

---

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| Build designer in v1 | Feature parity with Elsa, appealing to non-developers | Enormous scope; delays the SDK; designer quality requires dedicated UX investment |
| Defer designer to Phase 5 | Focus on SDK quality; ship faster; differentiate on code-first experience | Limits addressable market to developers |
| Ship a read-only dashboard only | Shows workflow state without design capability | Still significant scope; defers until Phase 4 |

---

## Decision

**No visual designer in v1. Code-first API only. Dashboard deferred to Phase 4. Designer deferred to Phase 5.**

---

## Why

1. **Scope discipline**: Building a production-quality designer is a separate product effort requiring a dedicated team, UX research, and significant testing. Including it in Phase 1 would delay the SDK — which is the foundation everything else builds on — by months.
2. **Target user alignment**: The `ProductStrategy.md` primary persona is a developer. Developers prefer code. A code-first API that "feels like EF Core" is more valuable to them than a designer. The designer is for business users — a future persona.
3. **The designer is Phase 5 for a reason**: A designer built on a solid state machine SDK is a better designer than one built alongside the SDK. The API we ship in Phase 2 informs the designer's model. Building the designer before the API is stable would produce a designer that constrains the API.
4. **Differentiation**: Most workflow tools lead with the designer. Arora.Workflow leads with the developer experience. This is a differentiated position, not a compromise.

---

## Trade-offs

- Developers who need a no-code designer today will choose Elsa or Camunda. This is documented and accepted in `ProductStrategy.md`.
- Until Phase 4, there is no visual monitoring of running instances. Developers use `GetHistoryAsync()` and their own UI.

---

## Consequences

- Phase 1 documentation contains no designer-related content.
- Phase 4 (dashboard) and Phase 5 (designer) are explicitly scoped in `Roadmap.md`.
- The `DefinitionJson` column in `WorkflowDefinitions` is the serialized state machine graph — intentionally designed to be renderable by a future designer.
- The public API for reading workflow definitions (`IWorkflowDefinitionService`) is part of Phase 2 to support future tooling.
