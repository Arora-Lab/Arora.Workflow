# Arora Labs — Arora.Workflow: Phase 1 Documentation Plan

## Background

You've been building Arora Brain as a standalone product. In parallel, through DealPilot, ERP discussions, and the Brain itself, you've discovered a pattern: every serious .NET business application needs the same infrastructure primitives — workflow, notifications, audit, identity, storage. You're proposing to extract those primitives into an independent, composable SDK suite called **Arora Labs**, starting with **Arora.Workflow**.

The analogy is exact: Microsoft didn't ship ASP.NET Identity bundled inside every MVC app. They built it as an independent, consumable library. Arora Labs is the same idea for modern .NET business applications.

**Arora Brain immediately benefits** — instead of baking workflow logic into the Brain codebase, it becomes a consumer of `Arora.Workflow`. This is the architectural integrity move.

---

## What Phase 1 Actually Is

Phase 1 is not a coding sprint. **Phase 1 is product design.**

We produce a documentation package that is thorough enough that a team of three engineers could independently implement the library from it. That standard — *team-implementable from docs alone* — is what separates a framework from a weekend project.

---

## Open Questions

> [!IMPORTANT]
> **Comparison Scope**
> The `Comparison.md` doc should compare Arora.Workflow against competitors.
> Confirmed competitors worth covering: **Elsa Workflows**, **Workflow Core**, **MassTransit Saga**, **NServiceBus Saga**, **Hangfire** (for task scheduling context).
> Should we also cover **Camunda** and **AWS Step Functions** to show the enterprise landscape?

> [!NOTE]
> **Benchmark Strategy**
> `Benchmark.md` — do you want this to be aspirational (target metrics we design to) or empirical (requires a working prototype to measure)? Recommendation: aspirational for Phase 1 with a note that empirical benchmarks follow Phase 2.

## Resolved Decisions

| Decision | Resolution |
|----------|------------|
| **Repository structure** | Separate repository per package. Brian will create the `Arora.Workflow` repository independently. |

---

## Proposed Changes

The output of Phase 1 is a complete `docs/` folder inside the `Arora.Workflow` repository.

---

### Repository Scaffold

#### [NEW] `Arora.Workflow/` (root directory)
Bare repository with only a `README.md`, `.gitignore`, and `docs/` folder. No source code.

#### [NEW] `README.md`
The public-facing "front door." One paragraph that answers: *What is Arora.Workflow? Who is it for? Why does it exist?* Links to `docs/Vision.md` and `docs/Roadmap.md`. This is the first thing an external developer reads.

---

### Core Documentation Package — `docs/`

#### [NEW] `docs/Vision.md`
**The philosophical document.** Answers the question "why does this exist?" at a product-company level.

Contents:
- The problem space: every .NET ERP, CRM, and business application reinvents workflow from scratch
- The positioning statement: *Workflow Platform for Business Applications*, not a generic workflow engine
- The seven principles (Developer First, Convention over Configuration, Opinionated, Auditable, Async-First, Database is Truth, EF Core Native)
- The differentiation story: why Arora.Workflow is not Elsa, not Hangfire, not MassTransit sagas
- The long-term ecosystem vision: how Arora.Workflow becomes the foundation of the Arora Labs suite

#### [NEW] `docs/Roadmap.md`
**The multi-year execution plan.**

Contents:
- **Year 1 — OSS Foundation**: SDK core, state machine engine, EF Core integration, NuGet release
- **Year 2 — Arora Brain adoption**: Arora Brain migrates its workflow needs to Arora.Workflow; real-world dogfooding
- **Year 3 — Expanded suite**: Arora.Notification, Arora.Audit released; cross-package integration examples
- **Year 4 — Platform**: Dashboard, designer, visual tooling, Arora Platform product
- Milestone definition: what "done" looks like per phase (API stability, test coverage targets, docs completeness)
- Deprecation policy: how breaking changes are versioned and communicated

#### [NEW] `docs/Architecture.md`
**The system design document.**

Contents:
- Component model: the SDK's internal layers (Domain, Application, Infrastructure, Extension Points)
- State machine engine design: how workflow state is defined, transitioned, and persisted
- Execution model: how steps execute (sync, async, retryable, compensatable)
- Persistence model: how workflow instances, step results, and history are stored via EF Core
- Extension points: `IWorkflowStep`, `IWorkflowMiddleware`, `IWorkflowEventHandler`, `IWorkflowDefinitionSource`
- Integration model: how host applications wire the SDK (`AddAroraWorkflow()`)
- Concurrency model: optimistic concurrency on workflow instances, idempotency keys on steps
- C4-style context, container, and component diagrams (as text/Mermaid)

#### [NEW] `docs/DDD.md`
**Domain-Driven Design model.** This is the authoritative glossary of the Arora.Workflow domain.

Contents:
- **Bounded context**: Arora.Workflow owns workflow orchestration. It does not own business entities (invoices, purchase orders) — those belong to the host application domain.
- **Ubiquitous language**: definitions of Workflow Definition, Workflow Instance, Step, Transition, State, Trigger, Actor, Approval, Escalation, History Entry, Deadline
- **Aggregate roots**: `WorkflowDefinition` and `WorkflowInstance` — fully defined with invariants
- **Entities**: `WorkflowStep`, `StepResult`, `Approval`, `EscalationPolicy`
- **Value objects**: `WorkflowState`, `TransitionGuard`, `RetryPolicy`, `DeadlineSpec`
- **Domain events**: `WorkflowStarted`, `StepExecuted`, `StepFailed`, `ApprovalRequested`, `ApprovalGranted`, `ApprovalRejected`, `WorkflowCompleted`, `WorkflowCancelled`, `WorkflowEscalated`
- **Domain services**: `WorkflowEngine`, `WorkflowDefinitionValidator`, `TransitionEvaluator`

#### [NEW] `docs/Database.md`
**Complete data model.**

Contents:
- Design philosophy: the database is the source of truth; no workflow state lives in memory alone
- Full schema: `WorkflowDefinitions`, `WorkflowInstances`, `WorkflowSteps`, `StepResults`, `Approvals`, `WorkflowHistory`, `EscalationPolicies`, `WorkflowDeadlines`
- Column-level definitions: type, nullability, default, index, constraint
- Soft-delete strategy: `IsDeleted` / `DeletedAt` on definitions and instances
- Audit columns: `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` on every table
- Multi-tenancy: all tables carry `TenantId` — Arora.Workflow is tenant-aware from day one (so Arora Brain can consume it without friction)
- EF Core mapping conventions: Global Query Filters for `TenantId` and `IsDeleted`, owned entity mappings for value objects
- Index strategy: which queries are hot, which indexes are required from launch

#### [NEW] `docs/PublicAPI.md`
**The API surface contract.** Written as if it is public NuGet documentation.

Contents:
- Registration API: `AddAroraWorkflow(builder, options => {...})`
- Workflow definition fluent API:
  ```csharp
  WorkflowDefinition.Create("invoice-approval")
      .Version(1)
      .WithStep<ValidateInvoiceStep>("validate")
      .WithApproval<ManagerApprovalStep>("manager-approval")
          .OnApprove(next: "process-payment")
          .OnReject(next: "notify-rejection")
          .WithEscalation(after: TimeSpan.FromDays(2), to: "finance-director")
      .WithStep<ProcessPaymentStep>("process-payment")
      .OnComplete(handler => ...)
  ```
- Instance management: `IWorkflowService.StartAsync()`, `GetInstanceAsync()`, `CancelAsync()`, `GetHistoryAsync()`
- Step authoring contract: `IWorkflowStep<TInput, TOutput>`, retry attributes, idempotency
- Approval API: `IApprovalService.ApproveAsync()`, `RejectAsync()`, `GetPendingApprovalsAsync()`
- Event subscription: `IWorkflowEventHandler<T>`
- Error model: typed exceptions (`WorkflowNotFoundException`, `InvalidTransitionException`, `StepExecutionException`)
- Extension points: how to register custom step middleware, definition sources, and persistence providers

#### [NEW] `docs/Examples.md`
**Working walkthrough of four canonical use cases.** This is what turns documentation into a framework people trust.

Each example contains:
1. Business problem description
2. State diagram (Mermaid)
3. Step-by-step workflow definition code
4. Key decision callouts (why this API shape, not another)

**Examples**:
1. **Invoice Approval** — linear multi-step approval with escalation deadline
2. **Vendor Onboarding** — parallel steps, conditional branching
3. **Purchase Order** — multi-level approval matrix, rejection loop
4. **Leave Request** — manager approval with automatic approver substitution

#### [NEW] `docs/ADR/`
A folder of individual ADR files, one per decision.

| File | Decision |
|------|----------|
| `ADR-001-state-machine-vs-flowchart.md` | Why a state machine model, not a flowchart/BPMN model |
| `ADR-002-efcore-native-vs-dapper.md` | Why EF Core native, not raw Dapper or multi-ORM |
| `ADR-003-no-designer-in-v1.md` | Why the v1 API is code-first; designer deferred to Phase 5 |
| `ADR-004-database-as-truth.md` | Why no in-memory-only workflow state; database is the only truth |
| `ADR-005-async-first-steps.md` | Why all steps are `async Task`; no sync step API |
| `ADR-006-multitenant-from-day-one.md` | Why `TenantId` is on every table, even for a personal project |
| `ADR-007-nuget-single-package-vs-metapackage.md` | Package structure decision: one package vs. split packages |
| `ADR-008-opinionated-escalation.md` | Why escalation is a first-class primitive, not a plugin |

Each ADR follows the standard format: Context → Options Considered → Decision → Why → Trade-offs → Consequences.

#### [NEW] `docs/Benchmark.md`
**Performance design targets** (aspirational for Phase 1).

Contents:
- Design goals: workflow start latency (<50ms p99), step execution overhead (<5ms), approval query (<20ms p99)
- Concurrency design target: 100 concurrent workflow instances without degradation
- Database query budget: no N+1 queries, explicit `.Include()` strategy for history loading
- Future empirical benchmark plan: BenchmarkDotNet setup, which scenarios to measure in Phase 2

#### [NEW] `docs/Comparison.md`
**Honest comparison with alternatives.** This is what engineering leads read before adopting a framework.

Comparison matrix covering:
- **Elsa Workflows** — powerful, visual, but complex setup; not opinionated for business apps
- **Workflow Core** — lightweight but abandoned; no EF Core v8+ support
- **MassTransit Saga** — excellent for message-driven systems; too heavyweight for simple approval flows
- **NServiceBus Saga** — enterprise-grade but commercial license required
- **Hangfire** — task scheduling, not workflow orchestration; often misused for workflow
- **Camunda** — BPM-first, Java roots, complex deployment; not .NET native
- **AWS Step Functions** — cloud lock-in; not embeddable

For each, the comparison covers: setup complexity, EF Core support, opinionated business patterns, license, activity level, and the honest "use this instead if..." recommendation.

---

## Coding Standards (to produce alongside docs)

#### [NEW] `docs/CodingStandards.md`
**How Arora.Workflow code is written.** This applies to the SDK source itself and sets the bar for contributions.

Contents:
- Naming conventions: namespaces, public API naming, exception naming
- "Feels like Microsoft wrote it" checklist: XML doc comments on every public member, options pattern, builder pattern, `ILogger<T>` injection, cancellation token propagation
- Test standards: unit tests for domain logic (no database), integration tests for EF Core behavior (real PostgreSQL via Testcontainers), naming convention `Method_Scenario_ExpectedResult`
- PR and commit standards
- Breaking change policy: SemVer, `[Obsolete]` before removal

---

## Document Production Order

The documents have dependencies. Produce them in this order:

```
1. Vision.md          ← sets the philosophy everything else inherits
2. DDD.md             ← defines the ubiquitous language everything references
3. Architecture.md    ← defines the system; references DDD terms
4. Database.md        ← derived from Architecture + DDD aggregates
5. PublicAPI.md       ← derived from Architecture; defines the developer surface
6. Examples.md        ← derived from PublicAPI; proves the API is usable
7. ADR/ (all)         ← written in parallel as decisions are made
8. Roadmap.md         ← written after Vision and Architecture are stable
9. Benchmark.md       ← written after Architecture defines the execution model
10. Comparison.md     ← written last; references all prior decisions
11. CodingStandards.md ← written alongside ADRs
12. README.md         ← written last; summarizes everything
```

---

## Verification Plan

### Document Quality Bar
Each document is held to this standard before it is considered complete:

| Criterion | Requirement |
|-----------|-------------|
| **Team-implementable** | A team of 3 engineers could build Phase 2 from this document alone |
| **No ambiguity** | Every term used is defined in `DDD.md` |
| **No orphan decisions** | Every major choice has a corresponding ADR |
| **Portfolio quality** | Reads like it came from a Microsoft engineering team |
| **API usability** | Every API in `PublicAPI.md` is validated by a working example in `Examples.md` |

### Manual Review
- Every document reviewed by the author against the quality bar above
- `PublicAPI.md` cross-referenced against `Examples.md` to ensure completeness
- All Mermaid diagrams rendered and verified in VS Code or GitHub preview
- `Comparison.md` cross-referenced against current GitHub activity of each competitor

---

## Connection Back to Arora Brain

Once `Arora.Workflow` Phase 1 documentation is complete, a follow-up ADR is added to the Arora Brain ADR log:

> **ADR-009 — Consume Arora.Workflow Instead of Building Workflow Internally**

This ADR documents the decision to make Arora Brain a *consumer* of Arora.Workflow, not a builder of workflow logic. This is the architectural moment that makes Arora Brain a real enterprise product rather than a self-contained monolith.
