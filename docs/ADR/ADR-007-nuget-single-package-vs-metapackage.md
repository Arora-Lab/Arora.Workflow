# ADR-007 — NuGet Package Structure: Single Package vs. Split Packages
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

Arora.Workflow could be shipped as a single NuGet package containing everything, or split into multiple packages where core and optional integrations are separate.

---

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| Single package | Simple dependency management, no split | Forces all consumers to take all dependencies (EF Core, MediatR, etc.) even if they use a different ORM |
| Split packages (core + integration packages) | Clean separation; consumers only take what they need | More packages to maintain; more complex versioning |
| Metapackage (all-in-one convenience + individual packages) | Easy onboarding, maximum flexibility | Additional complexity; may cause confusion |

---

## Decision

**Split packages. Core engine in `Arora.Workflow`. EF Core integration in `Arora.Workflow.EntityFramework`. Each plugin in its own package.**

---

## Why

1. **Dependency hygiene**: The core `Arora.Workflow` package should have the fewest possible dependencies. EF Core is a large dependency that not every consumer will want forced on them. An enterprise team with a custom ORM strategy should be able to implement `IWorkflowInstanceRepository` against their own data layer without pulling in EF Core.
2. **Plugin isolation**: Teams integrating Teams notifications should not pull in Slack SDK dependencies. The plugin model requires package-level separation.
3. **Mirrors the ASP.NET Core pattern**: `Microsoft.AspNetCore` is split into `Microsoft.AspNetCore.Authentication`, `Microsoft.AspNetCore.Authorization`, etc. This is the established .NET convention for library packages.
4. **Future-proofing**: When `Arora.Workflow.Dashboard` ships in Phase 4, it is a separate package with Blazor dependencies. When `Arora.Workflow.AI` ships, it carries OpenAI SDK dependencies. Separate packages prevent dependency explosion for teams that do not need those features.

---

## Package Structure

```
Arora.Workflow                     ← Core engine, interfaces, domain events (no EF Core)
Arora.Workflow.EntityFramework     ← EF Core persistence implementation
Arora.Workflow.Notifications       ← Notification plugin base interfaces
Arora.Workflow.Teams               ← Microsoft Teams adaptive card plugin
Arora.Workflow.Slack               ← Slack approval action plugin
Arora.Workflow.Dashboard           ← Phase 4: monitoring dashboard
Arora.Workflow.Testing             ← Test helpers (TestWorkflowHost, etc.)
```

---

## Trade-offs

- Most consumers will need `Arora.Workflow` + `Arora.Workflow.EntityFramework` at minimum. This is two packages instead of one. This is documented clearly in Getting Started.
- Versioning across packages must be kept in sync within a release (version `1.2.0` of all packages are compatible with each other). A shared `Directory.Build.props` enforces this.

---

## Consequences

- `Arora.Workflow` has zero production dependencies beyond MediatR and a logging abstraction (`Microsoft.Extensions.Logging.Abstractions`).
- `Arora.Workflow.EntityFramework` depends on `Arora.Workflow` and `Microsoft.EntityFrameworkCore`.
- All packages share the same version number per release. A version table in `Roadmap.md` documents compatibility.
- `Arora.Workflow.Testing` is a separate package with test infrastructure — it is never added to production projects.
