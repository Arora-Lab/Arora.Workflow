# Manifesto
# Arora.Workflow

**Version**: 1.0
**Status**: Accepted
**Date**: 2026-07-01

---

> *This document is the constitution of Arora.Workflow.*
> *Every design decision traces back to a line written here.*
> *When two approaches are technically equivalent, the Manifesto decides.*

---

## The Manifesto

**Workflow should be understandable.**

A developer should be able to read a workflow definition and immediately understand what it does. No proprietary DSL. No opaque XML. No runtime-only graph that cannot be inspected. If you cannot explain it in a code review, it is too complex.

---

**Workflow should be auditable.**

Every state. Every transition. Every approval. Every rejection. Every escalation. Every failure. All of it is recorded. Not as application logs — as structured, queryable data. If your auditor asks "who approved this invoice on March 14th, and why was it escalated?" — you have the answer in a database query, not a log file search.

---

**Workflow should be testable.**

A workflow definition is code. Code must be testable. You should be able to write a unit test that starts a workflow instance, drives it through every transition, and asserts the final state — without a database, without a running server, without infrastructure. If you cannot test it without ceremony, you will not test it at all.

---

**Workflow should be resilient.**

A process crash must not corrupt a workflow. A retry must not create a duplicate approval. A transient database error must not leave an instance in an unknown state. The engine guarantees at-least-once step execution with idempotency. The developer writes business logic, not retry plumbing.

---

**Workflow should not require months to learn.**

The learning curve is a product decision. Arora.Workflow is opinionated so that developers do not have to be. Invoice approval should take minutes. Vendor onboarding should take an afternoon. If a developer needs a week to understand how to configure an escalation policy, we have failed.

---

**Workflow should feel like Entity Framework.**

EF Core is the gold standard of .NET developer experience. It is opinionated enough to be productive immediately. It is extensible enough to support advanced scenarios. It maps naturally to C# idioms. Every API surface in Arora.Workflow is designed to feel like it belongs next to `DbContext` in a codebase. If it does not feel like EF Core, we redesign it.

---

**Developers should write business logic — not plumbing.**

The engine handles persistence, concurrency, retries, escalation timers, idempotency keys, and audit history. The developer handles the question that only they can answer: *what should happen when a manager approves an invoice?* That is the division of responsibility. The developer owns the "what." The engine owns the "how."

---

## What the Manifesto Is Not

The Manifesto does not describe features.
The Manifesto does not describe the roadmap.
The Manifesto does not describe the database schema.

It describes **what kind of product Arora.Workflow is allowed to become**.

If a proposed feature violates a line in this document, it is rejected — not because it is a bad feature, but because it is not *our* feature.

---

## Amendments

The Manifesto is a living document, but amendments are rare and require explicit justification.

A line may be added if a new principle emerges from real usage that cannot be derived from existing lines.

A line may never be removed. If a principle no longer applies, it is superseded by a new ADR that explains why the constraint was lifted.

All amendments are recorded in `docs/ADR/` before they take effect in the Manifesto.
