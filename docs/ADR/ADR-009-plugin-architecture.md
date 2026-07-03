# ADR-009 — Plugin Architecture: Small Core, Extensible Integrations
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

Arora.Workflow must integrate with many external systems over its lifetime: notification channels (email, Teams, Slack), AI routing, ERP systems (SAP, Oracle), dashboard UIs, and more. The question is how to structure these integrations.

---

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| Bake everything into the core | Simple, no plugin system needed | Core becomes bloated; every consumer takes every dependency |
| Plugin interface with all plugins in core repo | Organized but monolithic | All plugins must release together; dependency surface grows |
| Plugin architecture: core is small, integrations are separate packages | Core stays lean; integrations are opt-in; community can contribute plugins | More packages; plugin API must be stable |

---

## Decision

**Plugin architecture. The `Arora.Workflow` core is minimal. All integrations are separate packages implementing `IWorkflowPlugin`.**

---

## Why

1. **Core dependency hygiene** (also see ADR-007): Teams integrating Slack should not pull in Teams SDK. Teams integrating AI routing should not pull in OpenAI SDK if they are not using it.
2. **Community contribution path**: A plugin architecture gives external contributors a clear, bounded surface to contribute to. A contributor who wants to build an SAP integration does not need to modify the core — they implement `IWorkflowPlugin` in their own package.
3. **Future-proofing**: Arora.Workflow cannot anticipate every integration scenario. A plugin system ensures the core remains stable while the ecosystem grows.
4. **Isolation and testability**: Each plugin is independently testable. The Teams plugin can be tested without a full engine. The core engine can be tested without any plugins.
5. **Mirrors .NET ecosystem patterns**: ASP.NET Core, Serilog, MassTransit, and EF Core all use this pattern. It is the idiomatic .NET design for extensible libraries.

---

## Plugin Contract

```csharp
public interface IWorkflowPlugin
{
    string Name { get; }
    void ConfigureServices(IServiceCollection services, WorkflowOptions options);
    void Configure(IApplicationBuilder app);
}
```

Plugins register via the fluent builder:

```csharp
builder.Services
    .AddAroraWorkflow(...)
    .AddPlugin<TeamsNotificationPlugin>();

// Or via package-specific extension method (preferred):
    .AddTeamsNotifications(options => { ... });
```

---

## Built-In Extension Points

The core defines these extension points that plugins consume:

| Interface | Purpose |
|-----------|---------|
| `IWorkflowEventHandler<T>` | React to domain events (e.g., send notification on `ApprovalRequested`) |
| `IWorkflowMiddleware` | Intercept step execution |
| `IWorkflowDefinitionSource` | Load definitions from external sources (DB, API, file) |
| `IEscalationTargetResolver` | Custom logic for resolving escalation targets |
| `IActorResolver` | Custom logic for resolving `Dynamic` actor assignments |

---

## Trade-offs

- Plugin API must be stable. Breaking the `IWorkflowPlugin` contract is a major version bump.
- Plugin discovery is explicit (registered in DI), not automatic (no convention-based discovery). This is intentional — explicit is better than magic for infrastructure concerns.

---

## Consequences

- `Arora.Workflow` core has no knowledge of Teams, Slack, email, or AI.
- All communication between the engine and plugins happens through domain events.
- The plugin API (`IWorkflowPlugin`, all `IWorkflow*` extension interfaces) is part of the stable public API surface covered by semantic versioning guarantees.
- First-party plugins: `Arora.Workflow.Notifications`, `Arora.Workflow.Teams`, `Arora.Workflow.Slack`, `Arora.Workflow.AI` (all Phase 3+).
