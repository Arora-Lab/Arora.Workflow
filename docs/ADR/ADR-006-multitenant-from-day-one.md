# ADR-006 — Multi-Tenancy from Day One
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

The initial consumers of Arora.Workflow — including Arora Brain — are single-tenant applications. It would be simpler to skip multi-tenancy in Phase 1 and add it later when needed.

However, retrofitting multi-tenancy onto an existing schema and codebase is one of the most expensive and risky refactoring tasks in software engineering.

---

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| Single-tenant: no TenantId, add later | Simpler Phase 1 schema | Expensive and risky retrofit; existing data migration required |
| Multi-tenant from day one: TenantId on every table | Correct by construction; no future migration | Small overhead per table; tests must always set tenant context |

---

## Decision

**Multi-tenant from day one. Every entity carries a `TenantId`. EF Core Global Query Filters enforce isolation on all queries.**

---

## Why

1. **Arora.Workflow is a platform library, not an application**: Its purpose is to be consumed by multiple host applications, some of which will be multi-tenant SaaS products. If the library does not support multi-tenancy, it cannot be used by those applications without significant workarounds.
2. **Retrofitting is disproportionately expensive**: Adding `TenantId` to 8 tables — plus updating every query, every index, every migration, every test, every integration example — after Phase 2 would require touching every layer of the system. The cost is multiplicative, not additive.
3. **Arora Brain requires it**: Arora Brain is the primary Phase 2 consumer and is explicitly designed as a multi-tenant SaaS. If Arora.Workflow does not support multi-tenancy, Arora Brain cannot use it for the general case.
4. **Principal Engineer decision**: Building for the next state of the system, not just the current state, is how experienced engineers think. A single-tenant Phase 1 that requires a multi-tenancy migration in Phase 2 is not an engineering decision — it is technical debt creation.

---

## Trade-offs

- Every new entity must implement `ITenantScoped` and carry a `TenantId` column.
- Tests must always establish a `TenantContext` before running (a `TestWorkflowHost` test helper abstracts this).
- The `ITenantContext` interface must be implemented and registered by the host application.

---

## Consequences

- All 8 `aw_*` tables carry `TenantId uuid NOT NULL`.
- EF Core Global Query Filters on `TenantId` are applied to all entities in `OnModelCreating`.
- `ITenantContext` is an interface in `Arora.Workflow` that the host application implements (one line in most cases).
- The `TestWorkflowHost` test helper provides a default single-tenant context for unit and integration tests.
- No Arora.Workflow API returns data across tenant boundaries.
