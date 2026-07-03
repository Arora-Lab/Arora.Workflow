# SDK Design Guidelines
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01

---

> *The test for every API decision: if an ASP.NET Core / EF Core developer looks at this API,*
> *do they feel immediate familiarity? If the answer is no, redesign the API.*

---

## 1. The "Feels Like Microsoft" Standard

Arora.Workflow's API should feel like it was authored by the same team that wrote ASP.NET Core and Entity Framework Core. This is not just an aesthetic preference — it is a measurable adoption driver. When a developer already knows EF Core, they should be able to adopt Arora.Workflow in minutes, not hours, because the conventions are identical.

**What makes an API feel like Microsoft built it:**

1. Registration via the Options pattern, not constructor injection of configuration
2. Fluent builder APIs, not factory methods with many parameters
3. No static helpers — everything is injected via DI
4. Dependency injection is the only configuration mechanism
5. Async by default — no sync overloads without explicit justification
6. `CancellationToken` as the last parameter on every async method
7. `ILogger<T>` is always injectable, never `Console.Write`
8. Extension methods on `IServiceCollection` for registration
9. Extension methods on `IApplicationBuilder` / `WebApplication` for middleware
10. `[Obsolete]` with a message and a replacement path — never silent removal

---

## 2. Registration Pattern

Registration follows the established ASP.NET Core pattern exactly.

### Basic Registration

```csharp
// Program.cs
builder.Services.AddAroraWorkflow(options =>
{
    options.UseEntityFramework<AppDbContext>();
});

// Minimal middleware (if using HTTP triggers)
app.UseAroraWorkflow();
```

### Fluent Builder Pattern

```csharp
builder.Services
    .AddAroraWorkflow(options =>
    {
        options.UseEntityFramework<AppDbContext>();
        options.EscalationPollingInterval = TimeSpan.FromSeconds(30);
    })
    .AddTeamsNotifications(teams =>
    {
        teams.WebhookUrl = configuration["Teams:WebhookUrl"];
    })
    .AddSlackNotifications(slack =>
    {
        slack.BotToken = configuration["Slack:BotToken"];
    });
```

**Rules:**
- `AddAroraWorkflow()` returns `IWorkflowBuilder` (not `IServiceCollection`) to enable the fluent chain.
- Each plugin's extension method lives in its own NuGet package and extends `IWorkflowBuilder`.
- All configuration values are read from the options object, never from constructor parameters.
- Configuration can be bound from `IConfiguration`:

```csharp
builder.Services.AddAroraWorkflow(
    builder.Configuration.GetSection("AroraWorkflow"));
```

---

## 3. Workflow Definition API

The workflow definition API must be fluent, strongly typed, and readable without documentation. A developer should be able to read a workflow definition and understand what it does without prior training.

### Correct

```csharp
var definition = WorkflowDefinition
    .Create("invoice-approval")
    .Description("Standard invoice approval workflow")
    .Version(1)
    .WithStep<ValidateInvoiceStep>("validate")
        .WithRetry(maxAttempts: 3, delay: TimeSpan.FromSeconds(5))
    .WithApproval<ManagerApproval>("manager-approval")
        .AssignedTo(actor => actor.Role("Manager"))
        .OnApprove(next: "process-payment")
        .OnReject(next: "notify-rejection")
        .WithEscalation(after: TimeSpan.FromDays(2), to: actor => actor.Role("FinanceDirector"))
    .WithApproval<FinanceApproval>("finance-approval")
        .AssignedTo(actor => actor.Role("Finance"))
        .When(ctx => ctx.Input<InvoiceInput>().Amount > 10_000)
        .OnApprove(next: "process-payment")
        .OnReject(next: "notify-rejection")
    .WithStep<ProcessPaymentStep>("process-payment")
    .WithStep<SendConfirmationStep>("send-confirmation")
    .OnComplete(instance => instance.TransitionTo("Completed"))
    .Build();
```

### Wrong — avoid these patterns

```csharp
// ❌ Magic strings in routing without type safety
.Next("process-payment")   // what is "process-payment"? define the steps first

// ❌ XML or JSON configuration
WorkflowDefinition.FromJson("{ ... }");

// ❌ Static factory methods
WorkflowEngine.Register(new InvoiceApprovalWorkflow());

// ❌ 7-parameter constructors
new ApprovalStep("manager-approval", "Manager", null, TimeSpan.FromDays(2), "FinanceDirector", true, false);

// ❌ String-based actor assignment
.AssignedTo("Finance")  // prefer .AssignedTo(actor => actor.Role("Finance"))
```

---

## 4. Step Authoring Contract

Step implementations follow a single, consistent interface. There is no sync overload, no abstract base class requirement, and no magic attributes for the simplest case.

### Standard Step

```csharp
public class ValidateInvoiceStep : IWorkflowStep<ValidateInvoiceInput, ValidateInvoiceResult>
{
    private readonly IInvoiceValidator _validator;

    public ValidateInvoiceStep(IInvoiceValidator validator)
    {
        _validator = validator;
    }

    public async Task<ValidateInvoiceResult> ExecuteAsync(
        ValidateInvoiceInput input,
        CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(input.InvoiceId, cancellationToken);
        return new ValidateInvoiceResult(result.IsValid, result.Errors);
    }
}
```

### Retry via Attribute

```csharp
[RetryPolicy(MaxAttempts = 3, InitialDelaySeconds = 5, Backoff = BackoffStrategy.Exponential)]
public class ProcessPaymentStep : IWorkflowStep<PaymentInput, PaymentResult>
{
    // ...
}
```

**Rules:**
- Steps are registered in DI automatically by convention (scan for `IWorkflowStep<,>` in registered assemblies).
- Steps receive their dependencies via constructor injection. No service locator.
- Steps must not call the database directly. They receive their input; they return their output.
- Steps must not have side effects beyond their declared return type. Side effects go in event handlers.
- Steps must be safe to retry. If they are not idempotent by nature, they must check before acting.

---

## 5. Service APIs

Public services follow ASP.NET Core conventions exactly.

### IWorkflowService

```csharp
public interface IWorkflowService
{
    /// <summary>Starts a new workflow instance.</summary>
    /// <param name="request">The start request including the workflow name, version, and input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created workflow instance.</returns>
    Task<WorkflowInstance> StartAsync(
        StartWorkflowRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a workflow instance by ID.</summary>
    Task<WorkflowInstance?> GetInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the complete, ordered history for a workflow instance.</summary>
    Task<IReadOnlyList<WorkflowHistoryEntry>> GetHistoryAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a running workflow instance.</summary>
    Task CancelAsync(
        Guid instanceId,
        string reason,
        ActorInfo actor,
        CancellationToken cancellationToken = default);
}
```

### IApprovalService

```csharp
public interface IApprovalService
{
    /// <summary>Gets all pending approvals for the current actor.</summary>
    Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync(
        ActorInfo actor,
        CancellationToken cancellationToken = default);

    /// <summary>Submits an approval decision.</summary>
    Task ApproveAsync(
        Guid approvalId,
        ActorInfo actor,
        string? comment = null,
        CancellationToken cancellationToken = default);

    /// <summary>Submits a rejection decision.</summary>
    Task RejectAsync(
        Guid approvalId,
        ActorInfo actor,
        string? comment = null,
        CancellationToken cancellationToken = default);
}
```

**API Rules:**
- `CancellationToken` is the last parameter, with `= default` to make it optional for callers.
- Return `Task<T?>` for queries that may return no result (nullable reference type, not exception).
- Return `Task<T>` (non-nullable) for commands where the result is guaranteed on success.
- Never return `void` from an async method — always `Task`.
- Use record types for request/response DTOs.

---

## 6. Error Model

Errors are typed exceptions, not error codes or `Result<T, TError>` discriminated unions. This matches ASP.NET Core's own error model.

| Exception | When Thrown |
|-----------|-------------|
| `WorkflowNotFoundException` | No instance found for the provided ID |
| `WorkflowDefinitionNotFoundException` | No definition found for the provided name/version |
| `InvalidTransitionException` | No valid transition exists from the current state for the given trigger |
| `AmbiguousTransitionException` | More than one transition matches (authoring error) |
| `DuplicateApprovalException` | An approval decision was already submitted for this approval |
| `WorkflowAlreadyExistsException` | A workflow instance already exists for the provided idempotency key |
| `WorkflowInTerminalStateException` | Attempted to mutate a workflow instance that has already completed |
| `StepExecutionException` | A step threw an unhandled exception after exhausting retries |
| `WorkflowDefinitionValidationException` | A workflow definition failed validation on publish |

All exceptions include:
- A machine-readable `ErrorCode` property (e.g., `"WORKFLOW_NOT_FOUND"`)
- A human-readable `Message` property
- Structured context properties for logging

---

## 7. Naming Conventions

### Namespaces

```
Arora.Workflow                     ← public service interfaces, DTOs, exceptions
Arora.Workflow.Definition          ← WorkflowDefinition builder API
Arora.Workflow.Steps               ← IWorkflowStep<,>, step middleware
Arora.Workflow.Events              ← domain event types
Arora.Workflow.EntityFramework     ← EF Core integration (separate NuGet)
Arora.Workflow.Notifications       ← notification plugin (separate NuGet)
```

### Types

| Pattern | Example |
|---------|---------|
| Service interfaces | `IWorkflowService`, `IApprovalService` |
| Step interface | `IWorkflowStep<TInput, TOutput>` |
| Domain events | `WorkflowStarted`, `ApprovalGranted` |
| Exceptions | `WorkflowNotFoundException`, `InvalidTransitionException` |
| Request DTOs | `StartWorkflowRequest`, `ApproveRequest` |
| Response DTOs | `WorkflowInstance`, `PendingApproval` |
| Options | `WorkflowOptions`, `EscalationOptions` |
| Builder | `IWorkflowBuilder`, `IApprovalStepBuilder` |

### Avoid

- Abbreviations in public API names (`Wf`, `Aprv`, `Inst`)
- `Manager`, `Helper`, `Util`, `Handler` suffix on service types
- Generic names: `WorkflowService` (use `IWorkflowService`)
- Any public `static` helpers or extension methods not on `IServiceCollection` / `IApplicationBuilder`

---

## 8. XML Documentation

Every public type, method, and property must have XML documentation comments. No exceptions.

```csharp
/// <summary>
/// Starts a new workflow instance for the specified workflow definition.
/// </summary>
/// <param name="request">
/// The start request. Must include a valid workflow name and version.
/// The <see cref="StartWorkflowRequest.IdempotencyKey"/> is used to prevent duplicate instances.
/// If an instance with the same idempotency key already exists, it is returned without creating a new one.
/// </param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>
/// The created or existing <see cref="WorkflowInstance"/>.
/// </returns>
/// <exception cref="WorkflowDefinitionNotFoundException">
/// Thrown when no published definition exists for the specified name and version.
/// </exception>
Task<WorkflowInstance> StartAsync(
    StartWorkflowRequest request,
    CancellationToken cancellationToken = default);
```

These comments power IntelliSense, the NuGet package documentation, and the generated API reference. They are not optional.
