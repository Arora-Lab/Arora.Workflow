# ADR-005 — Async-First Steps (No Sync API)
# Arora.Workflow

**Date**: 2026-07-01
**Status**: Accepted

---

## Context

Step implementations could be offered as either synchronous (`TOutput Execute(TInput input)`) or asynchronous (`Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct)`) — or both via overloads.

---

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| Sync-only API | Simple, no async/await needed | Blocks threads, prevents I/O in steps, not scalable |
| Async-only API (Task) | Non-blocking, scalable, idiomatic modern .NET | Requires `async/await` knowledge |
| Both sync and async overloads | Maximum flexibility | Doubles the API surface; sync overloads will be misused |

---

## Decision

**Async-only. `IWorkflowStep<TInput, TOutput>` defines `Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken)`. No sync overloads.**

This is Principle 3 of the architecture (`Principles.md`).

---

## Why

1. **Steps perform I/O**: The primary purpose of most steps is to call an external system (database query, API call, payment processor). These are I/O operations. Synchronous I/O blocks a thread for the duration of the call. At any meaningful scale, this is unacceptable.
2. **ASP.NET Core is async**: The host application is ASP.NET Core, which is built entirely on async I/O. Introducing sync operations in workflow steps creates sync-over-async anti-patterns that degrade performance and stability.
3. **Cancellation token propagation**: `CancellationToken` is only meaningful in async code. By making the API async from day one, cancellation support is built in from day one — not retrofitted.
4. **Modern .NET idiom**: Since .NET 5+, the idiomatic .NET pattern for any operation that may perform I/O is `async Task`. Providing sync overloads would be teaching a pattern the ecosystem has moved away from.
5. **Forcing correctness**: If the step API is sync-only, a developer who needs to call `await httpClient.GetAsync(...)` inside a step will use `.GetAwaiter().GetResult()` — a deadlock waiting to happen. Async-only forces the correct pattern.

---

## Trade-offs

- Developers writing steps that do not perform I/O (pure computation) must still use `async Task`. They can use `Task.FromResult()` or mark the method `async` with no `await`. This is a minor inconvenience.
- The `async void` anti-pattern is forbidden in Arora.Workflow code. The Roslyn analyzer rule `AW001: Step must not use async void` catches this at compile time.

---

## Consequences

- `IWorkflowStep<TInput, TOutput>` has exactly one method: `Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken)`.
- `IWorkflowService`, `IApprovalService`, and all engine services expose only `async Task` methods.
- `CancellationToken` is the last parameter on every async method, with `= default` for callers who do not need to cancel.
- A Roslyn analyzer (`Arora.Workflow.Analyzers`) enforces: no `Task.Wait()`, no `.Result`, no `async void` in step implementations.
