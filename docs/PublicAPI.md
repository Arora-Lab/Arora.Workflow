# Public API Reference
# Arora.Workflow

**Version**: 1.0
**Status**: Draft
**Date**: 2026-07-01

> *This document is the authoritative contract for the Arora.Workflow public API surface.*
> *Every type, method, and property listed here is guaranteed stable across minor versions.*
> *Breaking changes require a major version bump and a deprecation cycle.*

---

## 1. Registration

### `IServiceCollection.AddAroraWorkflow()`

Registers all Arora.Workflow core services into the dependency injection container.

**Signatures:**

```csharp
// With action-based configuration
public static IWorkflowBuilder AddAroraWorkflow(
    this IServiceCollection services,
    Action<WorkflowOptions> configure);

// With IConfiguration section binding
public static IWorkflowBuilder AddAroraWorkflow(
    this IServiceCollection services,
    IConfiguration configuration);
```

**Returns:** `IWorkflowBuilder` — the fluent builder for registering plugins.

**Example:**

```csharp
builder.Services.AddAroraWorkflow(options =>
{
    options.UseEntityFramework<AppDbContext>();
    options.EscalationPollingInterval = TimeSpan.FromSeconds(60);
});
```

---

### `WorkflowOptions`

```csharp
public sealed class WorkflowOptions
{
    /// <summary>Configures Arora.Workflow to use EF Core for persistence.</summary>
    public WorkflowOptions UseEntityFramework<TContext>()
        where TContext : DbContext;

    /// <summary>
    /// How often the escalation scheduler polls for elapsed deadlines.
    /// Default: 60 seconds.
    /// </summary>
    public TimeSpan EscalationPollingInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Assemblies to scan for IWorkflowStep implementations.
    /// Defaults to the entry assembly.
    /// </summary>
    public IList<Assembly> StepAssemblies { get; }

    /// <summary>
    /// The clock abstraction used for deadline calculations.
    /// Override in tests with a deterministic clock.
    /// </summary>
    public Func<DateTimeOffset> Clock { get; set; } = () => DateTimeOffset.UtcNow;
}
```

---

### `IApplicationBuilder.UseAroraWorkflow()`

Registers the Arora.Workflow middleware pipeline (required only when using HTTP-triggered workflows or the built-in approval endpoints).

```csharp
public static IApplicationBuilder UseAroraWorkflow(
    this IApplicationBuilder app);
```

---

### `IWorkflowBuilder`

The fluent interface returned by `AddAroraWorkflow()`.

```csharp
public interface IWorkflowBuilder
{
    IServiceCollection Services { get; }

    /// <summary>Registers a workflow definition.</summary>
    IWorkflowBuilder AddWorkflow<TDefinition>()
        where TDefinition : class, IWorkflowDefinitionProvider;

    /// <summary>Registers step middleware at the end of the pipeline.</summary>
    IWorkflowBuilder UseStepMiddleware<TMiddleware>()
        where TMiddleware : class, IWorkflowMiddleware;

    /// <summary>Registers a plugin.</summary>
    IWorkflowBuilder AddPlugin<TPlugin>()
        where TPlugin : class, IWorkflowPlugin;
}
```

---

## 2. Workflow Definition API

### `WorkflowDefinition.Create()`

Entry point for the fluent definition builder.

```csharp
public static IWorkflowDefinitionBuilder Create(string name);
```

**Example:**

```csharp
var definition = WorkflowDefinition
    .Create("invoice-approval")
    .Description("Standard two-level invoice approval")
    .Version(1)
    .WithStep<ValidateInvoiceStep>("validate")
    .WithApproval<ManagerApprovalStep>("manager-approval")
        .AssignedTo(actor => actor.Role("Manager"))
        .OnApprove(next: "finance-approval")
        .OnReject(next: "send-rejection")
        .WithEscalation(after: TimeSpan.FromDays(2), to: actor => actor.Role("FinanceDirector"))
    .WithApproval<FinanceApprovalStep>("finance-approval")
        .AssignedTo(actor => actor.Role("Finance"))
        .When(ctx => ctx.Input<InvoiceInput>().Amount > 10_000)
        .OnApprove(next: "process-payment")
        .OnReject(next: "send-rejection")
    .WithStep<ProcessPaymentStep>("process-payment")
    .WithStep<SendConfirmationStep>("send-confirmation")
    .Build();
```

---

### `IWorkflowDefinitionBuilder`

```csharp
public interface IWorkflowDefinitionBuilder
{
    IWorkflowDefinitionBuilder Description(string description);
    IWorkflowDefinitionBuilder Version(int version);

    /// <summary>Adds a standard (non-approval) step.</summary>
    IStepBuilder<TStep> WithStep<TStep>(string name)
        where TStep : class, IWorkflowStep;

    /// <summary>Adds an approval step that pauses execution for a human decision.</summary>
    IApprovalStepBuilder<TStep> WithApproval<TStep>(string name)
        where TStep : class, IApprovalStep;

    WorkflowDefinition Build();
}
```

---

### `IStepBuilder<TStep>`

```csharp
public interface IStepBuilder<TStep> : IWorkflowDefinitionBuilder
{
    /// <summary>
    /// Configures a guard condition. This step is only executed when the predicate returns true.
    /// </summary>
    IStepBuilder<TStep> When(Expression<Func<WorkflowContext, bool>> guard);

    /// <summary>Configures the retry policy for this step.</summary>
    IStepBuilder<TStep> WithRetry(int maxAttempts, TimeSpan delay,
        BackoffStrategy backoff = BackoffStrategy.Fixed);

    /// <summary>
    /// Explicitly sets the next step on success.
    /// If not set, the next step in definition order is used.
    /// </summary>
    IStepBuilder<TStep> Then(string nextStepName);
}
```

---

### `IApprovalStepBuilder<TStep>`

```csharp
public interface IApprovalStepBuilder<TStep> : IWorkflowDefinitionBuilder
{
    /// <summary>Sets the actor(s) responsible for this approval.</summary>
    IApprovalStepBuilder<TStep> AssignedTo(Action<IActorSelector> configure);

    /// <summary>Guard condition — this approval step is only entered when the predicate is true.</summary>
    IApprovalStepBuilder<TStep> When(Expression<Func<WorkflowContext, bool>> guard);

    /// <summary>The step to transition to when approved.</summary>
    IApprovalStepBuilder<TStep> OnApprove(string next);

    /// <summary>The step to transition to when rejected.</summary>
    IApprovalStepBuilder<TStep> OnReject(string next);

    /// <summary>
    /// Configures automatic escalation if no decision is made within the specified duration.
    /// </summary>
    IApprovalStepBuilder<TStep> WithEscalation(
        TimeSpan after,
        Action<IActorSelector> to,
        EscalationAction action = EscalationAction.Escalate);
}
```

---

### `IActorSelector`

```csharp
public interface IActorSelector
{
    IActorSelector Role(string roleName);
    IActorSelector UserId(string userId);
    IActorSelector Dynamic(Func<WorkflowContext, string> resolver);
}
```

---

## 3. Step Authoring

### `IWorkflowStep<TInput, TOutput>`

The primary interface for implementing workflow steps.

```csharp
public interface IWorkflowStep<TInput, TOutput>
{
    /// <summary>
    /// Executes the step. Must be idempotent — the engine may call this more than once
    /// if the first execution succeeds but the result cannot be confirmed.
    /// </summary>
    Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken);
}
```

**Notes:**
- Steps are discovered by convention. Any class implementing `IWorkflowStep<,>` in a registered assembly is automatically available.
- Steps are resolved from the DI container per-execution. They are registered as transient by default.
- Steps must not call `IWorkflowService` or `IApprovalService`. The engine calls steps; steps do not call the engine.

---

### `[RetryPolicy]` Attribute

Applies a retry policy to a step at the class level (can be overridden in the definition builder).

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class RetryPolicyAttribute : Attribute
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialDelaySeconds { get; set; } = 5;
    public BackoffStrategy Backoff { get; set; } = BackoffStrategy.Exponential;
}
```

---

### `IWorkflowMiddleware`

Intercepts step execution. Executed in registration order.

```csharp
public interface IWorkflowMiddleware
{
    Task<TOutput> InvokeAsync<TInput, TOutput>(
        TInput input,
        WorkflowStepContext context,
        WorkflowStepDelegate<TInput, TOutput> next,
        CancellationToken cancellationToken);
}
```

---

## 4. Runtime Services

### `IWorkflowService`

Injected into controllers, handlers, and services to manage workflow instances.

```csharp
public interface IWorkflowService
{
    Task<WorkflowInstance> StartAsync(
        StartWorkflowRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkflowInstance?> GetInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    Task<WorkflowInstance?> GetByCorrelationIdAsync(
        string correlationId,
        string workflowName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowHistoryEntry>> GetHistoryAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid instanceId,
        string reason,
        ActorInfo actor,
        CancellationToken cancellationToken = default);
}
```

---

### `StartWorkflowRequest`

```csharp
public sealed record StartWorkflowRequest
{
    /// <summary>The name of the workflow definition to start.</summary>
    public required string WorkflowName { get; init; }

    /// <summary>
    /// The version of the definition to use. If null, the latest published version is used.
    /// </summary>
    public int? Version { get; init; }

    /// <summary>
    /// A caller-provided key that prevents duplicate workflow instances.
    /// If a workflow with this key already exists, it is returned without creating a new one.
    /// Example: use the business entity ID (e.g., invoice ID).
    /// </summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>A reference to the business entity this workflow is about.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>The serializable input object passed to the first step.</summary>
    public object? Input { get; init; }

    /// <summary>The actor who is starting the workflow.</summary>
    public required ActorInfo InitiatedBy { get; init; }
}
```

---

### `IApprovalService`

```csharp
public interface IApprovalService
{
    Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync(
        ActorInfo actor,
        CancellationToken cancellationToken = default);

    Task<PendingApproval?> GetApprovalAsync(
        Guid approvalId,
        CancellationToken cancellationToken = default);

    Task ApproveAsync(
        Guid approvalId,
        ActorInfo actor,
        string? comment = null,
        CancellationToken cancellationToken = default);

    Task RejectAsync(
        Guid approvalId,
        ActorInfo actor,
        string? comment = null,
        CancellationToken cancellationToken = default);
}
```

---

## 5. Domain Events

Implement `INotificationHandler<TEvent>` (MediatR) to handle workflow domain events.

```csharp
// Example: Send a Teams notification when an approval is requested
public class SendTeamsApprovalCardHandler
    : INotificationHandler<ApprovalRequested>
{
    public async Task Handle(
        ApprovalRequested notification,
        CancellationToken cancellationToken)
    {
        // notification.ApprovalId
        // notification.WorkflowInstanceId
        // notification.StepName
        // notification.AssignedActorId
        // notification.DeadlineAt
    }
}
```

**Full domain event catalog:**

```csharp
public record WorkflowStarted(Guid WorkflowInstanceId, string WorkflowName, int Version,
    string CorrelationId, ActorInfo InitiatedBy, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record WorkflowTransitioned(Guid WorkflowInstanceId, string FromState, string ToState,
    string? StepName, ActorInfo? Actor, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record StepExecuted(Guid WorkflowInstanceId, string StepName, int AttemptNumber,
    int DurationMs, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record StepFailed(Guid WorkflowInstanceId, string StepName, int AttemptNumber,
    string ErrorMessage, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record ApprovalRequested(Guid WorkflowInstanceId, Guid ApprovalId, string StepName,
    string AssignedActorId, DateTimeOffset? DeadlineAt, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record ApprovalGranted(Guid WorkflowInstanceId, Guid ApprovalId,
    ActorInfo DecisionActor, string? Comment, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record ApprovalRejected(Guid WorkflowInstanceId, Guid ApprovalId,
    ActorInfo DecisionActor, string? Comment, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record WorkflowEscalated(Guid WorkflowInstanceId, Guid ApprovalId,
    string FromActorId, string ToActorId, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record WorkflowCompleted(Guid WorkflowInstanceId, string CorrelationId,
    int DurationMs, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record WorkflowRejected(Guid WorkflowInstanceId, string CorrelationId,
    string RejectedAtStep, ActorInfo RejectedBy, DateTimeOffset OccurredAt)
    : IWorkflowEvent;

public record WorkflowCancelled(Guid WorkflowInstanceId, string CorrelationId,
    string Reason, ActorInfo CancelledBy, DateTimeOffset OccurredAt)
    : IWorkflowEvent;
```

---

## 6. Common Types

```csharp
/// <summary>Represents the actor who performed an action.</summary>
public sealed record ActorInfo(string Id, string DisplayName);

/// <summary>The current state and metadata of a workflow instance.</summary>
public sealed record WorkflowInstance
{
    public Guid Id { get; init; }
    public string WorkflowName { get; init; }
    public int WorkflowVersion { get; init; }
    public string CorrelationId { get; init; }
    public string CurrentState { get; init; }
    public WorkflowStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

public enum WorkflowStatus
{
    Running,
    PendingApproval,
    Completed,
    Rejected,
    Cancelled
}

/// <summary>A pending approval awaiting a decision.</summary>
public sealed record PendingApproval
{
    public Guid ApprovalId { get; init; }
    public Guid WorkflowInstanceId { get; init; }
    public string WorkflowName { get; init; }
    public string CorrelationId { get; init; }
    public string StepName { get; init; }
    public ActorInfo AssignedActor { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? DeadlineAt { get; init; }
}
```

---

## 7. Exception Reference

| Exception | `ErrorCode` | When Thrown |
|-----------|-------------|-------------|
| `WorkflowNotFoundException` | `WORKFLOW_NOT_FOUND` | No instance found |
| `WorkflowDefinitionNotFoundException` | `DEFINITION_NOT_FOUND` | No definition for name/version |
| `InvalidTransitionException` | `INVALID_TRANSITION` | No valid transition from current state |
| `AmbiguousTransitionException` | `AMBIGUOUS_TRANSITION` | More than one transition matches |
| `DuplicateApprovalException` | `DUPLICATE_APPROVAL` | Decision already submitted |
| `WorkflowAlreadyExistsException` | `WORKFLOW_ALREADY_EXISTS` | Idempotency key already used |
| `WorkflowInTerminalStateException` | `TERMINAL_STATE` | Instance already completed/cancelled |
| `StepExecutionException` | `STEP_EXECUTION_FAILED` | Step failed after all retries |
| `WorkflowDefinitionValidationException` | `DEFINITION_INVALID` | Definition failed validation |
