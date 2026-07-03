# Product Strategy
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01
**Author**: Brian Arora

---

> *This document keeps Arora.Workflow from becoming everything to everyone.*
> *It defines who we build for, who we do not build for, and what problems we deliberately leave unsolved.*
> *Every roadmap decision is evaluated against this document first.*

---

## 1. Mission Statement

> **"The easiest way to build enterprise approval workflows in .NET."**

The word **easiest** is precise. Not "most powerful." Not "most flexible." Not "most complete." Easiest. This is a goal that can be measured — time-to-first-workflow — and it constrains every design decision we make.

---

## 2. Why Arora.Workflow Exists

### The Market Gap

The .NET ecosystem has workflow solutions at both extremes:

- **Too heavy**: Elsa, Temporal, Camunda, NServiceBus Saga. Powerful and flexible, requiring significant investment to configure, deploy, and learn.
- **Too light**: Hangfire, manual `Status` columns, ad-hoc state machines. Fast to start, impossible to maintain.

There is no opinionated, developer-friendly, EF Core-native workflow SDK that solves the specific problem every .NET business application has: **multi-step approval workflows with audit and escalation**.

That is the gap Arora.Workflow fills.

### The Differentiation

We are not competing on features. We are competing on **experience**.

| Product | Philosophy |
|---------|------------|
| Elsa | General-purpose workflow platform |
| Temporal | Distributed system orchestration |
| Hangfire | Background job scheduling |
| MassTransit | Message-driven workflow |
| Camunda | BPMN enterprise platform |
| NServiceBus | Enterprise service bus + sagas |
| Dapr Workflow | Cloud-native distributed workflow |
| **Arora.Workflow** | **Opinionated business workflow SDK** |

Our story is not "we have more features than Elsa." Our story is "you can build invoice approval in 10 minutes and it will be production-ready."

---

## 3. The Ideal User

### Primary — The .NET Business Application Developer

| Attribute | Detail |
|-----------|--------|
| **Tech stack** | ASP.NET Core, Entity Framework Core, SQL Server or PostgreSQL |
| **Application type** | ERP, CRM, procurement system, HR system, document management |
| **Team size** | 1–10 developers |
| **Workflow needs** | Multi-step approvals, escalations, audit history, rejection and rework loops |
| **Current solution** | Status columns, ad-hoc state machines, or no workflow at all |
| **Pain point** | Spent weeks building workflow infrastructure; it is fragile and untested |
| **Goal** | Install a package and have approval workflow running today |

### Secondary — The Architect Evaluating for Enterprise Adoption

An architect at a software consultancy or product company evaluating whether to adopt Arora.Workflow for multiple client projects. They have seen the "Status column" anti-pattern too many times.

They are looking for: clean API design, EF Core compatibility, multi-tenancy support, strong documentation, and an active maintainer. They will read `docs/Architecture.md`, `docs/PublicAPI.md`, and the ADRs before recommending adoption.

### Tertiary — The Open Source Contributor

A .NET developer who agrees with the philosophy and wants to extend the library — adding a Teams notification plugin, a Blazor dashboard component, or a new step type. They are looking for: clear contribution guidelines, a well-defined plugin architecture, and an active, responsive maintainer.

---

## 4. Who Is NOT the Target User

Being explicit about who we are not building for is as important as defining who we are building for.

### ❌ Developers Orchestrating Distributed Microservices

If your workflow spans multiple microservices communicating via a message broker, Arora.Workflow is the wrong tool. You need Temporal, MassTransit Saga, or NServiceBus Saga. Arora.Workflow is designed to run inside a single ASP.NET Core process, embedded in an existing application.

### ❌ Developers Who Need Visual No-Code Workflow Design

If your business users need to define and modify workflow definitions without code, Arora.Workflow v1 is not for you. The visual designer is Phase 5, not Phase 1. Today's target user is a developer who is comfortable writing C#.

### ❌ Developers Who Need BPMN Import/Export

BPMN is a standard for modeling business processes. It is valuable in organizations that use BPMN tooling. Arora.Workflow does not support BPMN and has no plans to. Our workflow definitions are C# code, not XML files.

### ❌ High-Throughput Event Stream Processing

If your "workflow" processes thousands of events per second or operates as a real-time event streaming pipeline, Arora.Workflow is not designed for that scale. It is designed for human-in-the-loop approval workflows where transitions happen over hours or days, not milliseconds.

### ❌ Non-.NET Environments

Arora.Workflow is a .NET library. It requires .NET 9+. There are no plans for a JavaScript SDK, a REST API gateway, or language bindings for other runtimes.

---

## 5. What Problems We Deliberately Do Not Solve

### Not Solving: Visual Workflow Designer (Phase 1)

A visual drag-and-drop workflow designer is a significant product investment. It belongs in Phase 5 after the SDK has proven adoption. Building a designer in Phase 1 would add enormous complexity and delay the foundational SDK that makes everything else possible.

### Not Solving: Real-Time Event Streaming

Workflows in Arora.Workflow are human-pace workflows. Steps complete over minutes, hours, or days. We are not building a Kafka-competitor or a Flink-competitor.

### Not Solving: Multi-Vendor Deployment (Cloud-Native)

We are not building a Kubernetes operator, a cloud-hosted SaaS workflow engine, or a cloud-agnostic deployment abstraction. The library runs inside your application. Your deployment is your concern.

### Not Solving: Built-In Business Rules Engine

Some workflow platforms include a rules engine (e.g., Camunda's DMN support). We are not building one. Transition guards in Arora.Workflow are C# predicates. If you need a business rules engine, compose Arora.Workflow with one.

### Not Solving: Workflow Marketplace

We are not building a library of pre-built workflow templates. The `Examples.md` document provides reference implementations, but there is no "install an HR workflow pack" feature.

---

## 6. Competitive Strategy

### Strategy: Win on Developer Experience

We do not win by having more features than Elsa or more scalability than Temporal. We win by being the library that a developer can adopt in ten minutes and trust in production on day one.

This means:
- Documentation that is genuinely better than competitors
- An API that requires zero configuration to get started
- An error model that tells you what went wrong and how to fix it
- A Getting Started guide that ends with a running workflow, not a "next steps" page

### Strategy: Own the "EF Core Developer" Persona

EF Core developers are our primary audience. The API is designed to feel like EF Core. The documentation uses EF Core analogies. The integration model (`AddAroraWorkflow()` alongside `AddDbContext()`) is designed to feel like a natural extension of the EF Core ecosystem.

When an EF Core developer evaluates Arora.Workflow, they should feel immediate familiarity. That familiarity is a competitive moat.

### Strategy: Be the Reference Implementation for the Arora Labs Ecosystem

Arora.Workflow is the foundation that proves the Arora Labs model: independent, composable, opinionated .NET packages that work together. When `Arora.Brain` adopts `Arora.Workflow`, it demonstrates the ecosystem in action. That demonstration is itself a product differentiator.

---

## 7. Success Metrics

### Phase 1 (Documentation Complete)

| Metric | Target |
|--------|--------|
| All `docs/` documents complete and reviewed | ✅ Pass |
| `PublicAPI.md` fully cross-referenced with `Examples.md` | ✅ Pass |
| Invoice approval example completes in < 10 minutes | ✅ Pass |
| ADR written for every major architectural decision | ✅ Pass |

### Phase 2 (SDK 1.0 Released)

| Metric | Target |
|--------|--------|
| `dotnet add package Arora.Workflow` available on NuGet | ✅ Pass |
| Time-to-first-workflow for a new user | < 10 minutes |
| Unit test coverage | ≥ 80% (domain + application layers) |
| Integration test coverage | All public API surface paths covered |
| Build CI passes | ✅ Pass |

### Phase 3 (Adoption)

| Metric | Target |
|--------|--------|
| GitHub Stars (6 months post-release) | 100+ |
| External NuGet downloads | 500+ |
| Arora Brain migrated to Arora.Workflow | ✅ Complete |
| Zero P0 bugs open for > 7 days | ✅ Pass |

---

## 8. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| A new competitor ships a similar opinionated SDK | Medium | High | Ship first; documentation and developer experience are the moat |
| Elsa releases a significantly simpler API | Low | Medium | Our EF Core-native design and embedded model remain differentiators |
| Scope creep pulls us toward BPMN/designer too early | High | High | This document is the guardrail; reference it in every roadmap discussion |
| Maintainer bandwidth bottleneck | High | High | Plugin architecture keeps the core small; community extends via plugins |
| EF Core breaking changes in future .NET versions | Low | Medium | Integration tests with Testcontainers catch EF Core regressions early |
