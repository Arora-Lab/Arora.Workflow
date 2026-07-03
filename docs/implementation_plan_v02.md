# Arora Labs — Arora.Workflow: Phase 1 Documentation Plan
# Version 02

**Version**: 2.0
**Status**: Draft — Pending Final Approval
**Updated**: 2026-07-01

---

## Mission Statement

> **"The easiest way to build enterprise approval workflows in .NET."**

Notice what it doesn't say. Not fastest. Not most powerful. Not a replacement for Temporal or Elsa.
**Easiest.** That is a goal you can measure, and it aligns everything: opinionated defaults, great documentation, and a developer experience that feels like Microsoft built it.

---

## Background

Every serious .NET ERP, CRM, and business application reinvents the same workflow infrastructure from scratch. Arora.Workflow is the library that ends that. It is not a general-purpose workflow engine. It is an **opinionated workflow SDK for business applications**, built the way Microsoft would build it if they decided to solve this problem.

**Arora Brain immediately benefits** — instead of building workflow logic internally, it becomes a consumer of `Arora.Workflow`. This is the architectural moment that makes Arora Brain a real enterprise product rather than a self-contained monolith.

---

## What Phase 1 Actually Is

Phase 1 is not a coding sprint. **Phase 1 is product design.**

We produce a documentation package thorough enough that a team of three engineers could independently implement the library from it. That standard — *team-implementable from docs alone* — is what separates a framework from a weekend project.

---

## Resolved Decisions

| Decision | Resolution |
|----------|------------|
| **Repository structure** | Separate repository per package. Brian will create the `Arora.Workflow` repository independently. |
| **Benchmark strategy** | Aspirational (design targets) for Phase 1. Empirical benchmarks via BenchmarkDotNet follow in Phase 2 when working code exists. |
| **Competition framing** | Compare by **philosophy**, not just feature checklist. See `Comparison.md` scope below. |
| **Extensibility model** | Plugin architecture is a first-class concern from day one. Core stays small; integrations live in plugins. |

---

## Open Questions

> [!IMPORTANT]
> **Comparison Scope**
> Should `Comparison.md` include **Temporal.io** and **Dapr Workflow** alongside the confirmed set (Elsa, Workflow Core, MassTransit, NServiceBus, Hangfire, Camunda, AWS Step Functions)?
> Temporal in particular is gaining serious enterprise traction and is a meaningful philosophical contrast.

---

## Proposed Changes

The output of Phase 1 is a complete `docs/` folder. Below is the full document set — significantly expanded from v01.

---

### Repository Scaffold

#### [NEW] `README.md`
The public-facing front door. Must answer in under 60 seconds of reading:
- What is Arora.Workflow?
- Who is it for?
- Why does it exist?
- How do I get started?

Opens with the mission statement. Links to `docs/Vision.md`, `docs/Manifesto.md`, and `docs/Examples.md`. Contains the quickstart snippet — 10 lines of C# that produce a running invoice approval workflow.

---

### Core Documentation Package — `docs/`

#### [NEW] `docs/Manifesto.md`
**The philosophical constitution.** Every design decision in the library traces back to this document. Short, memorable, and opinionated.

```
Arora Workflow Manifesto

Workflow should be understandable.
Workflow should be auditable.
Workflow should be testable.
Workflow should be resilient.
Workflow should not require months to learn.
Workflow should feel like Entity Framework.
Developers should write business logic — not plumbing.
```

The manifesto is not marketing copy. It is the tiebreaker for every design debate.
When two approaches are both technically valid, the manifesto decides.

---

#### [NEW] `docs/Vision.md`
**The product-level story.** Answers "why does this exist?" at a company level.

Contents:
- The problem: every .NET ERP, CRM, and SaaS application reinvents approval workflow from scratch
- The mission statement and what it deliberately excludes
- The positioning: *Workflow Platform for Business Applications* vs. general-purpose engine
- The Arora Labs ecosystem: how `Arora.Workflow` becomes the foundation for `Arora.Notification`, `Arora.Audit`, `Arora.Documents`, and eventually `Arora.Brain`
- The success horizon: what does "winning" look like at Year 1, Year 3, Year 5?

---

#### [NEW] `docs/ProductStrategy.md`
**The most important document we weren't planning to write.** This is what keeps the product from becoming "everything to everyone."

Contents:

**Why Arora.Workflow over Elsa?**
Elsa is a general-purpose platform. It is powerful and flexible — and that flexibility is exactly what makes it hard to adopt for a specific problem. Arora.Workflow makes the hard decisions for you. Invoice approval should take minutes, not days of configuration.

**Who is the ideal user?**
A .NET developer building a business application (ERP, CRM, procurement, HR) who needs structured multi-step approval workflows, auditability, and escalation — and who wants to install a NuGet package, not deploy a workflow platform.

**Who is NOT the target user?**
- Developers who need visual BPMN diagramming (use Elsa or Camunda)
- Developers orchestrating distributed microservices at scale (use Temporal or MassTransit)
- Developers who need a general-purpose background job scheduler (use Hangfire)

**What problems are we deliberately not solving?**
- BPMN import/export
- Visual no-code designer (Phase 5, not Phase 1)
- Microservices saga orchestration
- Real-time event streaming

**Success metrics:**
- Time-to-first-workflow: < 10 minutes from `dotnet add package` to running invoice approval
- Documentation NPS: developers say "this feels like Microsoft built it"
- GitHub stars at 6 months: 100+ (realistic, not aspirational)
- Arora Brain adoption: Arora Brain fully migrated to Arora.Workflow by Year 2

---

#### [NEW] `docs/Principles.md`
**The architecture constitution.** Not just "how" — explains "why" for each principle. These are the five laws the codebase must never violate.

---

**Principle 1 — Database Is Truth**

Workflow state lives in the database. Always. The moment you allow in-memory-only state, you have a distributed systems problem disguised as a workflow problem. A crashed process must be able to resume from the database with no data loss.

*Consequence*: Every state transition is persisted before the transition is considered complete.

---

**Principle 2 — Workflow Is Deterministic**

Given the same workflow definition, the same inputs, and the same approvals — the outcome is always identical. No ambient state. No randomness in business logic paths.

*Consequence*: Steps must be pure functions over their inputs. Side effects (emails, notifications) happen via events, not inline.

---

**Principle 3 — Everything Asynchronous**

No blocking I/O. No sync-over-async. Every public API accepts a `CancellationToken` and returns `Task` or `Task<T>`.

*Consequence*: There is no synchronous step API. `IWorkflowStep<TInput, TOutput>` is `async Task<TOutput>` by definition.

---

**Principle 4 — Everything Idempotent**

Approving twice never breaks the workflow. Retrying a failed step never creates duplicate side effects. Each step carries an idempotency key enforced at the persistence layer.

*Consequence*: `IWorkflowStep` implementations must be written to be safely re-executed. The engine enforces idempotency; the step author is informed of this contract in documentation.

---

**Principle 5 — No Hidden State**

Every state transition is observable. Every approval, rejection, escalation, and failure is recorded in the workflow history. There is no internal engine state that cannot be read by the host application.

*Consequence*: `WorkflowHistory` is a first-class entity. The dashboard and audit log are built from real data, not reconstructed from logs.

---

#### [NEW] `docs/Architecture.md`
**The system design document.**

Contents:
- Component model: SDK internal layers (Domain, Application, Infrastructure, Extension Points)
- State machine engine design: how workflow state is defined, transitioned, and persisted
- Execution model: how steps execute (sync, async, retryable, compensatable)
- Persistence model: how instances, step results, and history are stored via EF Core
- **Plugin architecture** (new in v02): how the core stays small and integrations are opt-in
- Extension points: `IWorkflowStep`, `IWorkflowMiddleware`, `IWorkflowEventHandler`, `IWorkflowDefinitionSource`, `IWorkflowPlugin`
- Integration model: `AddAroraWorkflow()` + `UseAroraWorkflow()`
- Concurrency model: optimistic concurrency on workflow instances, idempotency keys on steps
- C4-style context, container, and component diagrams (Mermaid)

**Plugin Architecture section:**

The core package (`Arora.Workflow`) contains only the engine, persistence, and public API.
All integrations ship as separate, opt-in NuGet packages:

```
Arora.Workflow                    ← engine core
Arora.Workflow.Notifications      ← email/push notification hooks
Arora.Workflow.Teams              ← Microsoft Teams approval cards
Arora.Workflow.Slack              ← Slack approval actions
Arora.Workflow.AI                 ← AI-assisted routing and escalation
Arora.Workflow.Dashboard          ← Blazor/React dashboard component (Phase 4)
```

Each plugin registers via `AddAroraWorkflow().AddTeamsNotifications()` — the same fluent pattern throughout.

---

#### [NEW] `docs/SDKDesign.md`
**How the public API is designed.** Separate from coding standards — this is about the *feel* of the API surface.

Contents:

```
Public APIs must feel like ASP.NET Core.
Avoid static helpers.
Prefer dependency injection.
Favor fluent builders.
Avoid magic strings — prefer strongly typed alternatives.
CancellationToken on every async method.
ILogger<T> injected, never Console.WriteLine.
Async by default. No sync overloads unless Microsoft has one.
Options pattern for all configuration — no constructor with 7 parameters.
[Obsolete] before removal, never silent breaking changes.
```

Concrete API ergonomics requirements:

```csharp
// Registration — one line
builder.Services.AddAroraWorkflow(options => {
    options.UseEntityFramework<AppDbContext>();
    options.UseTeamsNotifications();
});

// Middleware — one line
app.UseAroraWorkflow();

// Definition — fluent, no XML, no JSON, no YAML
workflow
    .Step<ValidateInvoiceStep>("validate")
    .Approval<ManagerApprovalStep>("manager-approval")
        .OnApprove("process-payment")
        .OnReject("notify-rejection")
        .Escalate(after: TimeSpan.FromDays(2), to: "finance-director")
    .Step<ProcessPaymentStep>("process-payment");
```

*The test: if a developer who knows EF Core and ASP.NET Core looks at the API, they should feel immediately at home.*

---

#### [NEW] `docs/DDD.md`
**Domain-Driven Design model.** The authoritative glossary. Every document in the repo references this one.

Contents:
- **Bounded context**: Arora.Workflow owns workflow orchestration, not business entities
- **Ubiquitous language**: Workflow Definition, Workflow Instance, Step, Transition, State, Trigger, Actor, Approval, Escalation, History Entry, Deadline
- **Aggregate roots**: `WorkflowDefinition`, `WorkflowInstance`
- **Entities**: `WorkflowStep`, `StepResult`, `Approval`, `EscalationPolicy`
- **Value objects**: `WorkflowState`, `TransitionGuard`, `RetryPolicy`, `DeadlineSpec`
- **Domain events**: `WorkflowStarted`, `StepExecuted`, `StepFailed`, `ApprovalRequested`, `ApprovalGranted`, `ApprovalRejected`, `WorkflowCompleted`, `WorkflowCancelled`, `WorkflowEscalated`
- **Domain services**: `WorkflowEngine`, `WorkflowDefinitionValidator`, `TransitionEvaluator`

---

#### [NEW] `docs/Database.md`
**Complete data model.**

Contents:
- Design philosophy: database is truth (Principle 1)
- Full schema: `WorkflowDefinitions`, `WorkflowInstances`, `WorkflowSteps`, `StepResults`, `Approvals`, `WorkflowHistory`, `EscalationPolicies`, `WorkflowDeadlines`
- Column-level definitions: type, nullability, default, index, constraint
- Soft-delete strategy: `IsDeleted` / `DeletedAt`
- Audit columns: `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` on every table
- Multi-tenancy: all tables carry `TenantId` — EF Core Global Query Filters enforce isolation
- Index strategy: hot query paths identified upfront, not after performance problems

---

#### [NEW] `docs/PublicAPI.md`
**The API surface contract.** Written as public NuGet documentation. Every type, method, and exception in the public surface is documented here before a line of code is written.

Sections:
- Registration and middleware API
- Workflow definition fluent API (full builder grammar)
- Instance management: `IWorkflowService`
- Approval management: `IApprovalService`
- Step authoring: `IWorkflowStep<TInput, TOutput>`
- Event subscription: `IWorkflowEventHandler<T>`
- Error model: typed exceptions with when-to-catch guidance
- Plugin registration: `IWorkflowPlugin`

---

#### [NEW] `docs/Examples.md`
**The killer demo.** This is the document that turns documentation into a framework people trust — and the thing that most OSS projects get wrong.

The primary example is the headline:

> **Build an Invoice Approval System in 10 Minutes**

This immediately resonates with business developers. It is not a "Hello World." It is a real scenario they recognize.

All four examples include:
1. Business problem (2 sentences)
2. State diagram (Mermaid)
3. Full working workflow definition
4. Key API decisions called out inline

**Examples**:
1. **Invoice Approval** — linear approval with 2-day escalation (the headline demo)
2. **Vendor Onboarding** — parallel steps, conditional branching
3. **Purchase Order** — multi-level approval matrix, rejection loop
4. **Leave Request** — manager approval with automatic substitution

---

#### [NEW] `docs/ADR/` — Architecture Decision Records

| File | Decision |
|------|----------|
| `ADR-001-state-machine-vs-flowchart.md` | Why state machine, not BPMN/flowchart |
| `ADR-002-efcore-native-vs-dapper.md` | Why EF Core native |
| `ADR-003-no-designer-in-v1.md` | Why code-first; designer deferred to Phase 5 |
| `ADR-004-database-as-truth.md` | Why no in-memory-only state |
| `ADR-005-async-first-steps.md` | Why all steps are `async Task` |
| `ADR-006-multitenant-from-day-one.md` | Why `TenantId` on every table |
| `ADR-007-nuget-single-package-vs-metapackage.md` | Package structure decision |
| `ADR-008-opinionated-escalation.md` | Why escalation is a first-class primitive |
| `ADR-009-plugin-architecture.md` | Why the core stays small; integrations are plugins |
| `ADR-010-no-xml-no-yaml.md` | Why the API is pure C# — no configuration files |

---

#### [NEW] `docs/Roadmap.md`
**The multi-year execution plan.**

```
Year 1 — OSS Foundation
  SDK core, state machine engine, EF Core integration
  Invoice approval demo (the killer demo)
  Full docs package complete
  NuGet release: Arora.Workflow 1.0

Year 2 — Arora Brain Adoption
  Arora Brain migrates workflow needs to Arora.Workflow
  Real-world dogfooding of the SDK
  Arora.Workflow.Notifications plugin released

Year 3 — Expanded Suite
  Arora.Notification released
  Arora.Audit released
  Cross-package integration examples

Year 4 — Platform
  Dashboard (Phase 4)
  Designer (Phase 5)
  Arora Platform product
```

---

#### [NEW] `docs/Comparison.md`
**Honest comparison by philosophy, not feature checklist.**

The framing that matters:

| Product | Philosophy |
|---------|------------|
| Elsa | General-purpose workflow platform |
| Temporal | Distributed system orchestration |
| Hangfire | Background job scheduling |
| MassTransit | Message-driven workflow |
| Camunda | BPMN enterprise platform |
| NServiceBus | Enterprise service bus + sagas |
| **Arora Workflow** | **Opinionated business workflow SDK** |

That philosophical contrast is the lead. The feature matrix follows.

For each competitor: setup complexity, EF Core support, opinionated business patterns, license, GitHub activity, and the honest "use this instead of Arora.Workflow if..." recommendation.

---

#### [NEW] `docs/Benchmark.md`
**Performance design targets** (aspirational for Phase 1; empirical via BenchmarkDotNet in Phase 2).

- Workflow start latency: < 50ms p99
- Step execution overhead: < 5ms (engine cost, excluding step logic)
- Approval query: < 20ms p99
- Concurrency: 100 concurrent workflow instances without degradation
- No N+1 queries — explicit `.Include()` strategy defined upfront

---

#### [NEW] `docs/CodingStandards.md`
**How Arora.Workflow code is written.**

The test: *Does it feel like Microsoft wrote it?*

- XML doc comments on every public member — no exceptions
- Options pattern for all configuration
- Builder pattern for all fluent APIs
- `ILogger<T>` injected everywhere — no `Console.Write`, no static loggers
- `CancellationToken` on every async method signature
- `[Obsolete]` before removal, SemVer strictly followed
- Test naming: `Method_Scenario_ExpectedResult`
- Unit tests: domain logic with no database
- Integration tests: EF Core behavior with real PostgreSQL via Testcontainers

---

## Document Production Order

Dependencies are strict. This order is not optional.

```
1.  Manifesto.md        ← constitution; every other doc inherits from this
2.  Vision.md           ← why it exists; sets the product story
3.  ProductStrategy.md  ← who it's for, who it's not for, success metrics
4.  Principles.md       ← the 5 laws; referenced by every technical doc
5.  DDD.md              ← ubiquitous language; every technical doc references this
6.  Architecture.md     ← system design; references Principles + DDD
7.  Database.md         ← derived from Architecture + DDD aggregates
8.  SDKDesign.md        ← derived from Architecture; defines the feel of the API
9.  PublicAPI.md        ← derived from SDKDesign; the full public surface
10. Examples.md         ← derived from PublicAPI; proves the API works
11. ADR/ (all)          ← written in parallel as each decision is locked
12. Roadmap.md          ← written after Vision + Architecture are stable
13. Benchmark.md        ← written after Architecture defines the execution model
14. Comparison.md       ← written last; references all prior decisions
15. CodingStandards.md  ← written alongside ADRs
16. README.md           ← written last; the front door that references everything
```

---

## Verification Plan

### Document Quality Bar

| Criterion | Requirement |
|-----------|-------------|
| **Team-implementable** | A team of 3 engineers could build Phase 2 from this document set alone |
| **Manifesto-traceable** | Every design decision traces back to a Manifesto line or Principle |
| **No ambiguity** | Every term used is defined in `DDD.md` |
| **No orphan decisions** | Every major choice has a corresponding ADR |
| **Portfolio quality** | Reads like it came from a Microsoft engineering team |
| **API usability** | Every API in `PublicAPI.md` is demonstrated by a working example in `Examples.md` |
| **Killer demo works** | Invoice approval example runs in < 10 minutes from scratch |

### Manual Review
- Every document reviewed against the quality bar above
- `PublicAPI.md` cross-referenced against `Examples.md`
- All Mermaid diagrams rendered and verified in VS Code / GitHub preview
- `Comparison.md` verified against current GitHub activity of each competitor

---

## Connection Back to Arora Brain

Once Phase 1 documentation is complete, one ADR is added to the Arora Brain ADR log:

> **ADR-009 — Consume Arora.Workflow Instead of Building Workflow Internally**

This is the architectural moment that makes Arora Brain a real enterprise product rather than a self-contained monolith. Arora Brain does not build workflow. Arora Brain uses workflow.

That relationship — `Arora.Brain` as a *customer* of `Arora.Workflow` — validates the entire Arora Labs ecosystem vision.
