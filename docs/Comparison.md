# Comparison: Arora.Workflow vs. Alternatives
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01

---

> *This document compares Arora.Workflow against alternatives honestly.*
> *The goal is not to claim superiority — it is to help developers choose the right tool.*
> *If another tool is genuinely better for your use case, we say so.*

---

## 1. The Framework First: Compare by Philosophy

Before comparing features, compare what each tool is *for*. A tool built for the wrong problem will frustrate you no matter how many features it has.

| Product | Philosophy | Primary Use Case |
|---------|-----------|-----------------|
| **Elsa Workflows** | General-purpose workflow platform | Any workflow, visual or code-defined |
| **Temporal** | Distributed system orchestration | Long-running code across microservices |
| **Hangfire** | Background job scheduling | Delayed and recurring tasks |
| **MassTransit Saga** | Message-driven workflow | Workflows coordinated via message broker |
| **NServiceBus Saga** | Enterprise service bus + sagas | Enterprise messaging with saga patterns |
| **Camunda** | BPMN enterprise platform | BPMN-modeled business processes |
| **Dapr Workflow** | Cloud-native distributed workflow | Microservice orchestration on Dapr runtime |
| **Workflow Core** | Lightweight .NET workflow library | Simple sequential workflows |
| **Arora.Workflow** | **Opinionated business workflow SDK** | **Multi-step approvals in .NET business apps** |

---

## 2. Philosophy Comparison

### Arora.Workflow vs. Elsa Workflows

**Elsa's philosophy**: Maximum flexibility. Support every workflow pattern, any integration, visual and code-defined, with a rich designer.

**Arora.Workflow's philosophy**: Opinionated for business approval workflows. Fewer options, faster time-to-production.

| Dimension | Elsa | Arora.Workflow |
|-----------|------|----------------|
| Time to first workflow | Hours to days (configuration-heavy) | < 10 minutes |
| API complexity | High (many concepts: activities, triggers, bookmarks) | Low (steps, approvals, escalations) |
| Visual designer | ✅ Built-in | ❌ Phase 5 (code-first only in v1) |
| EF Core integration | Supported (via plugin) | Native (first-class) |
| Opinionated defaults | ❌ Flexible by design | ✅ Convention over configuration |
| Business approval patterns | Requires manual assembly | Built-in (approval, escalation, history) |
| License | MIT | MIT (planned) |
| Activity | Active | Active |

**Use Elsa instead of Arora.Workflow if:**
- You need a visual designer today
- You need maximum flexibility in workflow modeling
- Your team already uses Elsa and knows it well
- You need BPMN import/export

**Use Arora.Workflow instead of Elsa if:**
- You want an invoice approval running in 10 minutes
- You want a codebase that reads like EF Core + ASP.NET Core
- You are building a business application, not a workflow platform

---

### Arora.Workflow vs. Temporal

**Temporal's philosophy**: Fault-tolerant, distributed orchestration of long-running code. Your code IS the workflow — functions are automatically retried, replayed, and resumed across failures and infrastructure changes.

**Arora.Workflow's philosophy**: An embedded SDK for human-pace approval workflows. No separate service. No replay semantics. The database is truth.

| Dimension | Temporal | Arora.Workflow |
|-----------|----------|----------------|
| Deployment model | Separate Temporal cluster (server + workers) | Embedded in host application |
| Learning curve | High (new paradigm: deterministic replay) | Low (familiar .NET DI + EF Core) |
| Infrastructure required | Temporal server, separate worker service | Just a database |
| Approval workflow support | Manual implementation | Built-in |
| Audit history | Application responsibility | Built-in `WorkflowHistory` |
| Scale | Millions of concurrent workflows | Hundreds to thousands |
| Human-in-the-loop | Supported (signals) | Built-in (approval API) |
| License | MIT | MIT (planned) |

**Use Temporal instead of Arora.Workflow if:**
- You are orchestrating code across multiple microservices
- You need fault-tolerant long-running workflows (days, weeks, years)
- You are processing high-throughput event streams
- Your team is comfortable with distributed systems complexity

**Use Arora.Workflow instead of Temporal if:**
- You are building a monolithic or modular-monolith .NET application
- You need approval workflows with escalation, history, and audit
- You do not want to deploy and maintain a separate infrastructure component

---

### Arora.Workflow vs. Hangfire

**Hangfire's philosophy**: Background job scheduling. Queue a job, execute it later, retry on failure.

**Arora.Workflow's philosophy**: Multi-step approval orchestration with state, history, and human decisions.

| Dimension | Hangfire | Arora.Workflow |
|-----------|---------|----------------|
| Core concept | Background job | Workflow instance with states |
| Human approval | ❌ Not supported | ✅ Built-in |
| State machine | ❌ No | ✅ Yes |
| Audit history | ❌ No | ✅ Built-in |
| Escalation | ❌ No | ✅ Built-in |
| EF Core integration | Supported | Native |
| License | LGPL (free) / commercial | MIT (planned) |

**Use Hangfire instead of Arora.Workflow if:**
- You need background job scheduling (send email at 3 AM, run a report weekly)
- You have no human approval step in your process
- You need a fire-and-forget retry mechanism

**Use Arora.Workflow instead of Hangfire if:**
- Your workflow pauses for a human decision
- You need a complete audit trail of every state transition
- You are building approval flows, not background job queues

---

### Arora.Workflow vs. MassTransit Saga

**MassTransit's philosophy**: Message-driven workflow. Sagas coordinate state in response to messages on a message broker.

**Arora.Workflow's philosophy**: An embedded SDK with no message broker dependency.

| Dimension | MassTransit Saga | Arora.Workflow |
|-----------|-----------------|----------------|
| Message broker required | ✅ Yes (RabbitMQ, Azure Service Bus, etc.) | ❌ No |
| EF Core integration | Supported | Native |
| Human approval pattern | Manual implementation | Built-in |
| Audit history | Application responsibility | Built-in |
| Developer experience | Familiar to messaging developers | Familiar to EF Core developers |
| Learning curve | High (messaging concepts + saga patterns) | Low |

**Use MassTransit Saga instead of Arora.Workflow if:**
- You are already using MassTransit for messaging
- Your workflow is triggered by and produces messages
- You need event-driven saga patterns across services

**Use Arora.Workflow instead of MassTransit Saga if:**
- You do not have a message broker
- You want approval workflows without learning messaging patterns
- Your workflow is contained within a single application

---

### Arora.Workflow vs. NServiceBus Saga

| Dimension | NServiceBus | Arora.Workflow |
|-----------|------------|----------------|
| License | Commercial | MIT (planned) |
| Infrastructure | Message broker required | Database only |
| Human approval | Manual | Built-in |
| Audit | NServiceBus Particular | Built-in |
| EF Core | Supported | Native |

**Use NServiceBus if:**
- Your organization is already licensed for NServiceBus
- You need enterprise-grade messaging guarantees
- You are building a complex, distributed saga pattern

---

### Arora.Workflow vs. Camunda

**Camunda's philosophy**: BPMN-first. Business analysts model processes visually; Camunda executes them. Strong enterprise tooling, process analytics, and compliance support.

| Dimension | Camunda | Arora.Workflow |
|-----------|---------|----------------|
| Definition format | BPMN XML | C# fluent API |
| Visual designer | ✅ Enterprise-grade | ❌ Phase 5 |
| .NET SDK | Available (v8+) | Native |
| Deployment | Separate Camunda server | Embedded |
| Learning curve | High (BPMN model + Camunda platform) | Low |
| Business user tooling | ✅ Strong | ❌ Phase 5 |
| License | Community (limited) / Enterprise (paid) | MIT (planned) |

**Use Camunda instead of Arora.Workflow if:**
- Business analysts (not developers) define workflows
- You need BPMN compliance for regulatory or governance requirements
- You need enterprise process analytics and SLA reporting

---

### Arora.Workflow vs. Dapr Workflow

**Dapr's philosophy**: Cloud-native building blocks for distributed applications. Dapr Workflow orchestrates activities across Dapr-enabled services.

| Dimension | Dapr Workflow | Arora.Workflow |
|-----------|--------------|----------------|
| Infrastructure | Dapr sidecar required | Database only |
| .NET SDK | ✅ Available | Native |
| Human approval | Manual (via external signals) | Built-in |
| Cloud-native | ✅ Yes | ❌ Embedded in host |
| Audit history | Application responsibility | Built-in |

**Use Dapr Workflow if:**
- Your application is built on the Dapr runtime
- You are in a Kubernetes / cloud-native environment
- You need polyglot workflow orchestration (Python + .NET + Go)

---

### Arora.Workflow vs. Workflow Core

**Workflow Core's philosophy**: A lightweight, code-first .NET workflow library.

| Dimension | Workflow Core | Arora.Workflow |
|-----------|--------------|----------------|
| Activity | ⚠️ Low (last release 2022) | Active |
| EF Core v8+ | ❌ Not supported | ✅ Native |
| Human approval | Manual | Built-in |
| Escalation | Manual | Built-in |
| Audit | Manual | Built-in |
| .NET 9 support | ❌ | ✅ |

**Use Arora.Workflow instead of Workflow Core if:**
- You need .NET 8+ / .NET 9+ support
- You need built-in approval and escalation
- You need an actively maintained library

---

## 3. Feature Matrix Summary

| Feature | Arora.Workflow | Elsa | Temporal | Hangfire | MassTransit | Camunda |
|---------|:--------------:|:----:|:--------:|:--------:|:-----------:|:-------:|
| Code-first definition | ✅ | ✅ | ✅ | N/A | ✅ | ❌ (BPMN) |
| Visual designer | Phase 5 | ✅ | ❌ | ❌ | ❌ | ✅ |
| Human approval built-in | ✅ | ⚠️ Manual | ⚠️ Signals | ❌ | ⚠️ Manual | ✅ |
| Escalation built-in | ✅ | ⚠️ Manual | ⚠️ Manual | ❌ | ⚠️ Manual | ✅ |
| Audit history built-in | ✅ | ⚠️ Partial | ❌ | ❌ | ❌ | ✅ |
| EF Core native | ✅ | ✅ (plugin) | N/A | ✅ | ✅ | N/A |
| No infrastructure required | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ |
| < 10 min to first workflow | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| .NET 9 native | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Multi-tenancy | ✅ | ⚠️ Manual | N/A | ❌ | N/A | ⚠️ |
| License | MIT (planned) | MIT | MIT | LGPL/Paid | Apache 2 | Paid/Community |

---

## 4. Our Honest Recommendation

**Choose Arora.Workflow when:**
You are a .NET developer building a business application (ERP, CRM, procurement, HR) that needs multi-step approval workflows with audit, escalation, and a developer experience that feels like EF Core. You want to install a NuGet package and have a workflow running today.

**Choose something else when:**
- You need visual workflow design today → **Elsa** or **Camunda**
- You are orchestrating distributed microservices → **Temporal** or **Dapr Workflow**
- You need background job scheduling → **Hangfire**
- You have a message broker and messaging-first workflows → **MassTransit** or **NServiceBus**
