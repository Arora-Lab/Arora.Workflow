# ADR-002 — EF Core Native vs. Dapper / Multi-ORM
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

Arora.Workflow requires database persistence for workflow instances, approvals, history, and deadlines. The choice of data access technology has implications for developer experience, query complexity, and hosting requirements.

---

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| EF Core native | Familiar to .NET developers, LINQ, migrations, Global Query Filters, owned entities | Higher memory overhead than Dapper for raw queries |
| Dapper | Fastest raw performance, SQL control | Raw SQL strings, no Global Query Filters, migrations must be manual |
| Multi-ORM (EF Core + Dapper) | Best of both worlds per query | Two data access paradigms, increased complexity |

---

## Decision

**EF Core native.**

---

## Why

1. **Primary persona alignment**: The `ProductStrategy.md` target user is an EF Core developer. If Arora.Workflow uses EF Core internally, the persistence model feels familiar. They can open the Arora.Workflow source and read the entities without learning a new paradigm.
2. **Global Query Filters**: EF Core's Global Query Filter feature is the cleanest mechanism for enforcing tenant isolation (`TenantId`) and soft delete (`IsDeleted`) on every query without per-query boilerplate. Dapper has no equivalent.
3. **Migrations**: EF Core migrations ship with the package. The host application runs them alongside their own migrations. Dapper requires raw SQL migration scripts.
4. **Conventions**: EF Core owned entities map cleanly to value objects (e.g., `RetryPolicy`, `DeadlineSpec`) without extra join tables.
5. **Testing**: EF Core supports Testcontainers-based integration tests against real databases. The same test infrastructure the host application uses works for Arora.Workflow integration tests.

---

## Trade-offs

- EF Core has measurable overhead over raw Dapper for high-throughput read queries. At the scale Arora.Workflow targets (business approval workflows, not stream processing), this overhead is negligible.
- Complex aggregate queries (e.g., "all pending approvals with instance metadata") require careful `.Include()` design to avoid N+1. This is documented in `Benchmark.md` and enforced by code review.

---

## Consequences

- All persistence uses EF Core. No Dapper, no raw ADO.NET in the production path.
- The host application's `DbContext` must include Arora.Workflow entities (via inheritance or composition — both supported).
- Integration tests use `Testcontainers.PostgreSql` or `Testcontainers.MsSql` for real database behavior.
- Performance-critical read paths (e.g., escalation poller, pending approvals query) are profiled explicitly and documented in `Benchmark.md`.
