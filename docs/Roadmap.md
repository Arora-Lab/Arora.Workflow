# Roadmap
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01

---

> *This is a living document. Timelines are directional, not contractual.*
> *Priorities may shift based on real-world usage, community feedback, and ecosystem changes.*
> *Every significant scope change is documented with an ADR or a roadmap amendment.*

---

## Mission Statement

> **"The easiest way to build enterprise approval workflows in .NET."**

---

## Year 1 — OSS Foundation

**Goal**: Ship `Arora.Workflow` 1.0 as a production-ready, open-source NuGet package.

### Phase 1 — Documentation (Current)

**Status**: In Progress

The documentation package defines the product before a line of code is written.

| Deliverable | Status |
|-------------|--------|
| `Manifesto.md` | ✅ Complete |
| `Vision.md` | ✅ Complete |
| `ProductStrategy.md` | ✅ Complete |
| `Principles.md` | ✅ Complete |
| `DDD.md` | ✅ Complete |
| `Architecture.md` | ✅ Complete |
| `Database.md` | ✅ Complete |
| `SDKDesign.md` | ✅ Complete |
| `PublicAPI.md` | ✅ Complete |
| `Examples.md` | ✅ Complete |
| `ADR/` (ADR-001 through ADR-010) | ✅ Complete |
| `Roadmap.md` | ✅ Complete |
| `Benchmark.md` | ✅ Complete |
| `Comparison.md` | ✅ Complete |
| `CodingStandards.md` | ✅ Complete |
| `README.md` | ✅ Complete |

**Quality bar**: A team of three engineers could build Phase 2 from this documentation package alone.

---

### Phase 2 — SDK Core

**Target**: Q3–Q4 Year 1

The working SDK. Code-first, no designer, no dashboard. Everything the documentation describes.

**Deliverables:**

- [ ] Solution structure: `Arora.Workflow`, `Arora.Workflow.EntityFramework`, `Arora.Workflow.Testing`
- [ ] Domain layer: `WorkflowDefinition`, `WorkflowInstance`, all value objects, domain events
- [ ] Application layer: `WorkflowEngine`, `IWorkflowService`, `IApprovalService`, all command/query handlers
- [ ] Infrastructure layer: EF Core entities, migrations, repositories, `EscalationScheduler`
- [ ] Fluent definition builder: `WorkflowDefinition.Create()...Build()`
- [ ] Step middleware pipeline: `IWorkflowMiddleware`, built-in middleware (logging, idempotency, retry)
- [ ] MediatR event dispatching: all 11 domain events wired
- [ ] Optimistic concurrency: `RowVersion` / `xmin` on `WorkflowInstances`
- [ ] Multi-tenancy: `ITenantContext`, Global Query Filters
- [ ] Test suite: unit (domain + application), integration (Testcontainers)
- [ ] Roslyn analyzer: `Arora.Workflow.Analyzers` (AW001, AW002)
- [ ] NuGet packaging: GitHub Actions CI/CD pipeline
- [ ] Documentation site: docfx or similar, hosted on GitHub Pages

**Milestone Definition: Phase 2 Complete**

- `dotnet add package Arora.Workflow` installs successfully
- Invoice approval example from `Examples.md` runs in under 10 minutes
- Unit test coverage ≥ 80% (domain + application layers)
- All public API paths covered by integration tests
- CI pipeline passes (build, test, pack, publish)
- No open P0 bugs

---

## Year 2 — Arora Brain Adoption

**Goal**: Arora Brain migrates its workflow needs to `Arora.Workflow`. Real-world dogfooding.

### Phase 3 — Production Validation

**Target**: Q1–Q2 Year 2

Arora.Workflow is used in production by a real application. Edge cases surface. API ergonomics are validated.

**Deliverables:**

- [ ] `Arora.Workflow.Notifications` plugin: `INotificationProvider` interface, email implementation
- [ ] Arora Brain ADR-009: formal decision to consume `Arora.Workflow`
- [ ] Arora Brain migration: all workflow logic moved from Brain to `Arora.Workflow`
- [ ] `Arora.Workflow` 1.1 released: incorporates lessons from Arora Brain production use
- [ ] Breaking change assessment: API surface evaluated for v2 candidates
- [ ] Community: GitHub Issues triaged, first external contributor PRs reviewed

**Key Validation Questions:**

- Does the API feel like EF Core in real usage?
- Is the escalation scheduler reliable under load?
- Are the integration tests catching regressions?
- What did the `Examples.md` not cover that real applications need?

---

## Year 3 — Expanded Suite

**Goal**: Arora.Notification and Arora.Audit released. Cross-package integration demonstrated.

### Phase 4 — Ecosystem Expansion

**Target**: Year 3

**Deliverables:**

- [ ] `Arora.Notification` 1.0: independent NuGet package for multi-channel notifications
  - Email, Teams, Slack, SMS providers
  - Template engine
  - Delivery tracking and retry
- [ ] `Arora.Audit` 1.0: independent NuGet package for structured audit logging
  - `AuditEvent` domain model
  - EF Core persistence
  - Query API: filter by actor, entity type, date range
- [ ] Cross-package integration: `Arora.Workflow` + `Arora.Notification` + `Arora.Audit` demonstrated in a full example application (the "Invoice System" reference app)
- [ ] `Arora.Workflow.Teams` 1.0: Microsoft Teams adaptive card approval plugin
- [ ] `Arora.Workflow.Slack` 1.0: Slack approval action plugin

---

## Year 4 — Platform

**Goal**: Visual tooling. Commercial potential established.

### Phase 5 — Dashboard (Visual Monitoring)

**Target**: Early Year 4

A browser-based dashboard for monitoring running workflow instances. For developers and operations teams.

**Scope:**
- Live view of all workflow instances (filter by status, definition, date)
- Instance detail: current state, pending approvals, history timeline
- Manual intervention: cancel a workflow, reassign an approval
- Metrics: average time-to-completion, escalation rate, rejection rate

**Technology decision**: Blazor or React — ADR to be written when this phase begins.

---

### Phase 6 — Designer (Visual Authoring)

**Target**: Late Year 4 / Year 5

A visual drag-and-drop workflow definition tool for business users.

**Scope:**
- Drag steps and approval nodes onto a canvas
- Connect with transitions, configure guards
- Preview the generated C# definition
- Export to code (not to JSON — the designer generates C# code)

**Design constraint**: The designer generates C# code. It does not introduce a new definition format. This keeps the runtime simple and maintains the code-first philosophy.

---

## Versioning Policy

Arora.Workflow follows [Semantic Versioning](https://semver.org/):

| Version bump | When |
|-------------|------|
| **Patch** (1.0.x) | Bug fixes, documentation corrections |
| **Minor** (1.x.0) | New features that are backward compatible |
| **Major** (x.0.0) | Breaking changes to the public API |

**Breaking Change Policy:**
1. `[Obsolete("Use X instead. This member will be removed in v2.")]` is added.
2. One minor version elapses with the obsolete marking.
3. The member is removed in the next major version.
4. An ADR is written for every breaking change explaining why and how to migrate.

---

## Deprecation Policy

A `WorkflowDefinition` version is deprecated when:
- A newer version is published
- The `Deprecate()` method is called on the definition

Deprecated definitions:
- Cannot be started (new instances rejected with `WorkflowDefinitionDeprecatedException`)
- Existing running instances continue to completion
- Are retained in the database indefinitely (audit requirements)

---

## Compatibility Matrix

| `Arora.Workflow` | .NET | EF Core | MediatR |
|-----------------|------|---------|---------|
| 1.x | .NET 9+ | 9.x | 12.x |
| 2.x | .NET 10+ | 10.x | TBD |
