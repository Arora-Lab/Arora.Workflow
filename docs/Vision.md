# Vision
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01
**Author**: Brian Arora

---

## Mission Statement

> **"The easiest way to build enterprise approval workflows in .NET."**

---

## 1. The Problem

Every .NET business application eventually needs workflow. Not "background jobs." Not "scheduled tasks." Actual, structured, auditable, multi-step workflow — with approvals, escalations, rejections, reassignments, and a complete history of everything that happened.

And every team builds it from scratch.

They start with a `Status` column on their `Invoice` table. Then they add a `ReviewedBy` column. Then an `ApprovedAt` column. Then a second approver level. Then an escalation email when nothing happens for three days. Then a history table when the auditor asks who approved what. Then a retry mechanism when the escalation email silently fails at 2 AM.

Six months later, they have a workflow engine — poorly designed, untested, coupled to their specific entity model, and unmaintainable. They did not set out to build a workflow engine. They set out to build an invoice system.

**Arora.Workflow is the library that ends this pattern.**

Install it. Define your workflow in C#. Let the engine handle persistence, concurrency, retries, escalation, and audit. You write the business logic. The engine writes the plumbing.

---

## 2. The Positioning

Arora.Workflow is **not** a general-purpose workflow engine.

General-purpose engines are powerful and flexible. They are also complex, require significant configuration, and make no assumptions about your problem domain. When everything is configurable, nothing works well by default.

Arora.Workflow is an **opinionated workflow SDK for business applications**. It makes decisions for you. An invoice approval workflow should take minutes to define, not days of configuration. That is only possible if the library has an opinion about what "invoice approval" looks like.

This is the same philosophy that made Entity Framework successful. EF Core does not support every possible database pattern. It supports the patterns that 90% of .NET applications need, and it makes those patterns excellent. Arora.Workflow does the same for approval workflows.

---

## 3. The Differentiation

### vs. Elsa Workflows

Elsa is a general-purpose workflow platform with a visual designer, BPMN support, and extensive configurability. It is an excellent product for teams that need maximum flexibility.

Arora.Workflow is for teams that do not need maximum flexibility — they need an invoice approved. The learning curve of Elsa is the price of its flexibility. Arora.Workflow trades flexibility for speed-to-production.

### vs. Temporal / Dapr Workflow

Temporal is a distributed system orchestration platform. It solves a fundamentally different problem: coordinating long-running operations across microservices at scale. It requires a dedicated server, a separate deployment, and a paradigm shift in how you think about code execution.

Arora.Workflow is an embedded SDK. No separate server. No new deployment. Add a NuGet package, call `AddAroraWorkflow()`, and your existing ASP.NET Core application has workflow.

### vs. MassTransit Saga / NServiceBus

These are message-driven saga implementations. They are the right choice for systems already built on message brokers. They assume a messaging infrastructure.

Arora.Workflow assumes only Entity Framework Core and a relational database. This is the right assumption for the vast majority of .NET business applications.

### vs. Hangfire

Hangfire is a background job scheduler. It is not a workflow engine. It has no concept of approval, state machine, or transition guards. Developers who use Hangfire for workflow have outgrown Hangfire.

---

## 4. The Arora Labs Ecosystem

Arora.Workflow is the first package in the Arora Labs suite — a set of independent, composable .NET SDK packages for enterprise business applications. Each package solves one problem extremely well, following the same design principles.

```
Arora Labs

├── Arora.Workflow         ← this repository
├── Arora.Notification     ← Year 3
├── Arora.Audit            ← Year 3
├── Arora.Documents        ← Year 4
├── Arora.Search           ← Year 4
├── Arora.Identity         ← Year 4
└── Arora.Brain            ← Year 2 (becomes a consumer of Arora.Workflow)
```

**Arora Brain is a customer, not a sibling.**

Arora Brain — the AI Personal Knowledge Operating System — was the proving ground for this idea. Its architecture requires workflow, notifications, and audit. Instead of building those capabilities inside the Brain, they are extracted into independent packages. Arora Brain becomes a consumer of Arora.Workflow. This is how real platform companies are built.

Each package is published independently to NuGet. Each package has independent versioning, independent documentation, and an independent release cycle. Packages know nothing about each other except through well-defined interfaces.

---

## 5. The Developer Experience Vision

Open the Arora.Workflow documentation. Five minutes of reading. Ten minutes of coding. Your first workflow is running.

```
 5 min  →  Read the Getting Started guide
10 min  →  Build your first invoice approval
30 min  →  Add escalation and reassignment
 1 hour →  Add a second workflow for vendor onboarding
 1 day  →  Production-ready
```

This is not marketing copy. It is a design constraint. If a first-time user cannot reach "running workflow" in ten minutes, the API is wrong — not the user.

Every documentation page is written with this constraint in mind. Every API decision is evaluated against it. The Getting Started guide is the most important document in the repository, and it is written before the implementation.

---

## 6. Success Horizon

### Year 1 — Foundation
- `Arora.Workflow` 1.0 published to NuGet as open source
- Documentation complete: every document in `docs/` is written, reviewed, and published
- Killer demo live: "Build an Invoice Approval System in 10 Minutes"
- GitHub: actively maintained, issues responded to within 48 hours

### Year 2 — Validation
- Arora Brain migrates its workflow needs to `Arora.Workflow`
- Real-world dogfooding surfaces edge cases and API improvements
- `Arora.Workflow` 1.1 released incorporating lessons from production use
- `Arora.Workflow.Notifications` plugin released

### Year 3 — Expansion
- `Arora.Notification` and `Arora.Audit` released as independent packages
- Cross-package integration examples published
- Community adoption: external developers using the package in their own projects

### Year 4 — Platform
- Visual dashboard (Phase 4): browser-based workflow instance monitoring
- Visual designer (Phase 5): no-code workflow definition for business users
- Arora Platform: the commercial offering built on top of the OSS core

---

## 7. What Success Looks Like

A developer who has never heard of Arora.Workflow opens the README, installs the NuGet package, and has a working invoice approval workflow running in their existing ASP.NET Core application in under ten minutes.

Six months later, that same developer tells a colleague: *"I was going to build this myself. I installed Arora.Workflow instead. It handles all the edge cases I would have missed."*

That is the product. That is the vision.
