# Coding Standards
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01

---

> *The test for every line of code: "Does this look like Microsoft wrote it?"*
> *If the answer is no, rewrite it.*

---

## 1. The Standard

Arora.Workflow code is held to the standard of Microsoft's own libraries (ASP.NET Core, Entity Framework Core, Microsoft.Extensions.*). This is not an aesthetic preference — it is a product requirement. When a developer browses the source code of Arora.Workflow, they should feel immediate familiarity.

Reference implementations to study:
- [aspnetcore](https://github.com/dotnet/aspnetcore) — API shape, DI registration, middleware patterns
- [efcore](https://github.com/dotnet/efcore) — fluent builder API, interceptors, value object mapping
- [extensions](https://github.com/dotnet/extensions) — `ILogger<T>`, `IOptions<T>`, `IHostedService`

---

## 2. Naming Conventions

### Namespaces

```
Arora.Workflow                     ← public API types (services, DTOs, exceptions)
Arora.Workflow.Definition          ← fluent definition builder
Arora.Workflow.Steps               ← step interfaces and middleware
Arora.Workflow.Events              ← domain event records
Arora.Workflow.Internal            ← internal engine implementation (not public API)
Arora.Workflow.EntityFramework     ← EF Core integration (separate NuGet)
```

Types in `Arora.Workflow.Internal.*` are never part of the public API. They may change without a version bump. Do not reference them from plugins or host applications.

### Types

| Category | Convention | Example |
|----------|-----------|---------|
| Service interfaces | `I[Noun]Service` | `IWorkflowService` |
| Repository interfaces | `I[Entity]Repository` | `IWorkflowInstanceRepository` |
| Domain events | `[Subject][Verb]` (past tense) | `WorkflowStarted`, `ApprovalGranted` |
| Exceptions | `[Subject][Problem]Exception` | `WorkflowNotFoundException` |
| Options | `[Feature]Options` | `WorkflowOptions`, `EscalationOptions` |
| Builders | `I[Feature]Builder` | `IWorkflowDefinitionBuilder`, `IApprovalStepBuilder` |
| Request DTOs | `[Action][Subject]Request` | `StartWorkflowRequest`, `ApproveRequest` |
| Response DTOs | Noun (no suffix) | `WorkflowInstance`, `PendingApproval` |
| Value objects | Noun (no suffix) | `WorkflowState`, `RetryPolicy` |
| Aggregate roots | Noun (no suffix) | `WorkflowDefinition`, `WorkflowInstance` |

### Avoid

- Abbreviations in public names: `WfInstance`, `AprvlSvc`
- `Manager`, `Helper`, `Util`, `Utils` suffixes
- `Get` prefix on properties: `instance.GetCurrentState()` → `instance.CurrentState`
- Generic type names: `Data`, `Info`, `Model` (except `ActorInfo` which is an established term in `DDD.md`)

---

## 3. XML Documentation

Every `public` and `protected` type, method, property, and constructor **must** have an XML documentation comment. No exceptions.

### Required Tags

```csharp
/// <summary>Brief description of what this member does.</summary>
/// <param name="paramName">What this parameter represents and any validation rules.</param>
/// <returns>What is returned, including null semantics (when can it be null?).</returns>
/// <exception cref="SomeException">When is this exception thrown?</exception>
/// <remarks>Optional: longer-form context or usage notes.</remarks>
```

### Quality Bar for XML Docs

- `<summary>` must be a complete sentence ending with a period.
- `<param>` must explain the *meaning* of the parameter, not restate its type.
- `<returns>` must describe when the return value is null (for nullable types).
- `<exception>` must describe the exact condition that triggers it.

**Bad:**
```csharp
/// <summary>Gets the workflow instance.</summary>
/// <param name="id">The id.</param>
Task<WorkflowInstance?> GetInstanceAsync(Guid id, CancellationToken ct);
```

**Good:**
```csharp
/// <summary>
/// Returns the workflow instance with the specified ID, or null if no instance exists.
/// </summary>
/// <param name="instanceId">
/// The unique identifier of the workflow instance to retrieve.
/// </param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>
/// The <see cref="WorkflowInstance"/>, or null if no instance with the given ID exists
/// in the current tenant.
/// </returns>
Task<WorkflowInstance?> GetInstanceAsync(
    Guid instanceId,
    CancellationToken cancellationToken = default);
```

---

## 4. Async Standards

- Every method that performs I/O returns `Task` or `Task<T>`. No exceptions.
- `CancellationToken cancellationToken = default` is the last parameter on every async public method.
- No `Task.Wait()`, `.Result`, or `.GetAwaiter().GetResult()` in production code — ever.
- No `async void` — ever. Event handlers that must be `async void` are wrapped in a `Task.Run` with exception logging.
- Method names for async operations end in `Async`: `StartAsync`, `ApproveAsync`, `GetInstanceAsync`.
- **Exception**: Properties may not be `async`. If a property requires async computation, refactor to a method.

---

## 5. Dependency Injection

- All services are injected via constructor injection. No property injection. No service locator.
- Service lifetimes follow ASP.NET Core conventions:
  - `IWorkflowService`, `IApprovalService`: **Scoped** (one per HTTP request / DI scope)
  - `WorkflowEngine`: **Scoped**
  - `IWorkflowDefinitionCache`: **Singleton**
  - `EscalationScheduler`: **Singleton** (it is an `IHostedService`)
  - Step implementations (`IWorkflowStep<,>`): **Transient** (a new instance per step execution)
- No `new` keyword for service types in production code. All dependencies are injected.

---

## 6. Error Handling

- Use typed, domain-specific exceptions (see `PublicAPI.md` for the full list). Never throw `Exception` or `ApplicationException`.
- All exceptions include:
  - A machine-readable `ErrorCode` string property
  - A human-readable `Message` (the default exception message)
  - Structured context properties (e.g., `InstanceId`, `StepName`)
- Do not swallow exceptions silently. If an exception is caught and not re-thrown, it is logged at `Error` or `Critical` level with full context.
- Validation errors return `WorkflowDefinitionValidationException` with a list of `ValidationFailure` items — never a generic exception.

```csharp
// Correct — typed exception with context
throw new WorkflowNotFoundException(instanceId);

// Wrong — generic
throw new Exception($"Workflow {instanceId} not found");

// Wrong — wrong type
throw new InvalidOperationException($"Workflow {instanceId} not found");
```

---

## 7. Logging

- Use `ILogger<T>` everywhere. No `Console.Write`. No static loggers. No `Debug.WriteLine`.
- Use structured logging with named parameters:

```csharp
// Correct
_logger.LogInformation(
    "Workflow {WorkflowInstanceId} transitioned from {FromState} to {ToState}",
    instance.Id, fromState, toState);

// Wrong
_logger.LogInformation($"Workflow {instance.Id} transitioned...");
```

- Log levels:
  - `Trace`: internal engine decisions (transition evaluation, cache hits)
  - `Debug`: step execution start/complete
  - `Information`: state transitions, approval decisions
  - `Warning`: retries, escalations, unexpected-but-recoverable conditions
  - `Error`: step failures, concurrency conflicts (with stack trace)
  - `Critical`: engine failures that corrupt workflow state (should never happen)

- Never log: user-provided workflow input/output data (may contain PII). Log only IDs, state names, and structured metadata.

---

## 8. Testing Standards

### Test Pyramid

```
        /\
       /  \        E2E Tests (minimal — verify host registration works)
      /    \
     /------\      Integration Tests (EF Core behavior, real PostgreSQL)
    /        \
   /----------\    Unit Tests (domain logic, application logic — no database)
```

### Unit Tests

- Target: domain layer (aggregate logic, value objects, domain services) and application layer (command/query handlers).
- No database. No HTTP. No EF Core.
- Use `TestWorkflowHost` (from `Arora.Workflow.Testing`) to host the engine in-memory.
- Naming convention: `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public async Task ApproveAsync_WhenInstanceInPendingState_TransitionsToNextStep()

[Fact]
public async Task ApproveAsync_WhenInstanceInTerminalState_ThrowsWorkflowInTerminalStateException()

[Fact]
public async Task StartAsync_WithDuplicateIdempotencyKey_ReturnsExistingInstance()
```

### Integration Tests

- Target: EF Core repositories, `WorkflowEngine` end-to-end, escalation scheduler.
- Use `Testcontainers.PostgreSql` (PostgreSQL) or `Testcontainers.MsSql` (SQL Server).
- Each test class gets a fresh database container (or a fresh schema, for speed).
- Use the `WorkflowIntegrationTestBase` base class from `Arora.Workflow.Testing`.

### Test Coverage Targets

| Layer | Target |
|-------|--------|
| Domain | ≥ 90% |
| Application | ≥ 80% |
| Infrastructure | ≥ 70% (EF Core integration tests) |
| Public API surface | 100% of public methods covered by at least one test |

---

## 9. Code Review Standards

### PR Requirements

- Every PR must include tests for new behavior.
- Every new public member must have XML documentation.
- Breaking changes require a corresponding ADR before the PR is merged.
- No PR is merged with failing CI (build, lint, test).

### What Reviewers Check

1. Does the code follow the naming conventions above?
2. Is there XML documentation on every new public member?
3. Are there unit tests for the new behavior?
4. Does the change require a new ADR?
5. Does the API feel like it belongs next to ASP.NET Core?

### PR Size

- Target: < 400 lines changed per PR.
- Large changes are split into a sequence of smaller PRs (infrastructure → behavior → tests → documentation).

---

## 10. Breaking Change Policy

1. **Identify**: A change is breaking if it removes or renames a public type, method, or property, or changes the signature of a public method.
2. **Deprecate**: Add `[Obsolete("Use X instead. This member will be removed in v{n+1}.")]` in the current minor version.
3. **Wait**: The deprecated member survives for at least one minor version.
4. **Remove**: Remove the deprecated member in the next major version.
5. **Document**: An ADR is written for every breaking change: what changed, why, and how to migrate.

---

## 11. Source Code Structure

```
src/
  Arora.Workflow/
    Domain/
      Aggregates/
      Events/
      Services/
      ValueObjects/
    Application/
      Features/
        Workflows/
          Start/
          Cancel/
          GetInstance/
        Approvals/
          Approve/
          Reject/
          GetPending/
      Common/
        Interfaces/
        Behaviors/
    Internal/
      Engine/
      Execution/
      Scheduling/

  Arora.Workflow.EntityFramework/
    DbContext/
    Configurations/
    Migrations/
    Repositories/

  Arora.Workflow.Testing/
    TestWorkflowHost/
    Builders/

tests/
  Arora.Workflow.UnitTests/
  Arora.Workflow.IntegrationTests/

benchmarks/
  Arora.Workflow.Benchmarks/
```
