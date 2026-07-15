# Arora.Workflow

> **The easiest way to build enterprise approval workflows in .NET.**

Arora.Workflow is an opinionated, EF Core-native workflow SDK for .NET business applications. Install it, define your workflow in C#, and have invoice approval running in under 10 minutes — without deploying a separate server, learning a new paradigm, or writing retry plumbing.

---

## Quickstart

```bash
dotnet add package Arora.Workflow
dotnet add package Arora.Workflow.EntityFramework
```

```csharp
// 1. Register
builder.Services.AddAroraWorkflow(options =>
    options.UseEntityFramework<AppDbContext>());

// 2. Define
var definition = WorkflowDefinition
    .Create("invoice-approval")
    .Version(1)
    .WithStep<ValidateInvoiceStep>("validate")
    .WithApproval<ManagerApproval>("manager-approval")
        .AssignedTo(a => a.Role("Manager"))
        .OnApprove(next: "process-payment")
        .OnReject(next: "send-rejection")
        .WithEscalation(after: TimeSpan.FromDays(2), to: a => a.Role("FinanceDirector"))
    .WithStep<ProcessPaymentStep>("process-payment")
    .Build();

// 3. Start
var instance = await workflowService.StartAsync(new StartWorkflowRequest
{
    WorkflowName  = "invoice-approval",
    IdempotencyKey = $"invoice-{invoiceId}",
    CorrelationId  = invoiceId.ToString(),
    Input          = new InvoiceInput(invoiceId, amount),
    InitiatedBy    = new ActorInfo(userId, userName)
});

// 4. Approve (when the manager acts)
await approvalService.ApproveAsync(approvalId, actor, comment: "Looks good");
```

That's it. Invoice approval is running, persisted, audited, and escalation-scheduled.

---

## Why Arora.Workflow

Every .NET business application eventually needs workflow. Teams start with a `Status` column. Then they add a reviewer field. Then a second approver level. Then escalation emails. Then a history table when the auditor asks. Six months later, they have a poorly designed workflow engine baked into their application.

Arora.Workflow is the library that ends this pattern.

| What you get | What you don't have to build |
|-------------|------------------------------|
| State machine engine | Retry and idempotency plumbing |
| EF Core persistence (shared DbContext) | Custom schema and migrations |
| Multi-tenancy via Global Query Filters | Per-query tenant filtering |
| Approval API (`Approve`, `Reject`, `GetPending`) | Approval routing logic |
| Automatic escalation with deadline timers | Timer infrastructure |
| Complete audit history (`WorkflowHistory`) | Audit log tables |
| Optimistic concurrency | Concurrent modification conflicts |

---

## Design Principles

1. **Developer First** — `AddAroraWorkflow()` should feel like Microsoft wrote it.
2. **Convention over Configuration** — defaults work for 90% of cases.
3. **Opinionated** — invoice approval takes minutes, not days of configuration.
4. **Everything Auditable** — every state, transition, and approval is recorded.
5. **Async First** — everything retryable, everything non-blocking.
6. **Database is Truth** — no in-memory-only state; no data loss on restart.
7. **EF Core Native** — feels like it belongs next to `DbContext` in your codebase.

---

## Documentation

| Document | Description |
|----------|-------------|
| [Manifesto](docs/Manifesto.md) | The philosophical constitution of Arora.Workflow |
| [Vision](docs/Vision.md) | Why it exists and where it is going |
| [Product Strategy](docs/ProductStrategy.md) | Who it is for, who it is not for |
| [Architecture Principles](docs/Principles.md) | The five laws the codebase must not violate |
| [DDD Model](docs/DDD.md) | Domain language, aggregates, events |
| [Architecture](docs/Architecture.md) | System design, engine, plugin model |
| [Database](docs/Database.md) | Complete schema and data model |
| [SDK Design](docs/SDKDesign.md) | API ergonomics and conventions |
| [Public API Reference](docs/PublicAPI.md) | Every type and method in the public surface |
| [Examples](docs/Examples.md) | Invoice Approval, Vendor Onboarding, PO, Leave Request |
| [ADR/](docs/ADR/) | Architecture Decision Records (10 decisions documented) |
| [Roadmap](docs/Roadmap.md) | Year 1 through Year 4 execution plan |
| [Comparison](docs/Comparison.md) | Arora.Workflow vs. Elsa, Temporal, Hangfire, Camunda, and more |
| [Benchmark](docs/Benchmark.md) | Performance design targets |
| [Coding Standards](docs/CodingStandards.md) | How Arora.Workflow code is written |

---

## Comparison at a Glance

| | Arora.Workflow | Elsa | Temporal | Hangfire |
|-|:-:|:-:|:-:|:-:|
| Code-first definition | ✅ | ✅ | ✅ | N/A |
| Visual designer | Phase 5 | ✅ | ❌ | ❌ |
| Human approval built-in | ✅ | Manual | Manual | ❌ |
| Escalation built-in | ✅ | Manual | Manual | ❌ |
| Audit history built-in | ✅ | Partial | ❌ | ❌ |
| EF Core native | ✅ | Plugin | N/A | ✅ |
| No extra infrastructure | ✅ | ✅ | ❌ | ✅ |
| Time to first workflow | < 10 min | Hours | Days | < 10 min |

Full comparison in [docs/Comparison.md](docs/Comparison.md).

---

## Arora Labs Ecosystem

Arora.Workflow is the first package in the **Arora Labs** suite — independent, composable .NET SDK packages for enterprise business applications.

```
Arora Labs

├── Arora.Workflow      ← this package
├── Arora.Notification  ← Year 3
├── Arora.Audit         ← Year 3
├── Arora.Documents     ← Year 4
└── Arora.Brain         ← Year 2 (consumes Arora.Workflow)
```

Each package solves one problem extremely well. Each is independent, versioned separately, and published to NuGet.

---

## Status

**Phase 1 — Documentation Complete.** All architecture documents, DDD model, API design, examples, and ADRs are written.

**Phase 2 — SDK Implementation.** In progress. NuGet release targeted for Q3–Q4 Year 1.

---

## License

Apache 2.0 — see [LICENSE](LICENSE).

---

## Author

Arora Lab — [GitHub](https://github.com/Arora-Lab) | [Website](https://arora-lab.com/)
