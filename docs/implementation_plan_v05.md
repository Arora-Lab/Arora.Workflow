# Arora Workflow — Phase 4 Implementation Plan

The objective of Phase 4 is to introduce durable, asynchronous background processing capabilities for Arora Workflow while maintaining the engine's core identity as a passive, deterministic state machine.

We will create a separate, optional package (`Arora.Workflow.BackgroundProcessing`) that hosts a worker to poll, claim, and process durable work items generated transactionally by the workflow engine. This design ensures that the engine does not hold hidden state, while still adding production-grade resilience, retries, and deadline processing.

## User Review Required
> [!IMPORTANT]
> The architectural plan has been updated to reflect your requirements for Phase 4 (durable work item queue, safe claiming, separate background package). Please review the updated plan below and confirm if you are ready to proceed with the implementation.

## Proposed Changes

### 1. New Project: `Arora.Workflow.BackgroundProcessing`
Create a new project that contains the hosted services and workers, ensuring that applications only needing request-driven workflows are not forced to reference or run background workers.

#### [NEW] [Arora.Workflow.BackgroundProcessing.csproj](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.BackgroundProcessing/Arora.Workflow.BackgroundProcessing.csproj)
- Class library containing `IHostedService` / `BackgroundService` implementations.
- References `Arora.Workflow` core.

#### [NEW] [WorkflowBackgroundService.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.BackgroundProcessing/Hosting/WorkflowBackgroundService.cs)
- The main `BackgroundService` that orchestrates polling loops.
- Configures separate handlers/loops for different work item types (e.g., executing steps, resuming workflows, processing deadlines) to avoid a monolithic worker class.
- Implements graceful shutdown: stops claiming work, finishes/abandons current items, releases leases, and propagates the `CancellationToken`.

#### [NEW] [BackgroundProcessingExtensions.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.BackgroundProcessing/Extensions/BackgroundProcessingExtensions.cs)
- Exposes `AddBackgroundProcessing(options => ...)` on the `IWorkflowBuilder` to register the worker services.
- Allows configuring `PollingInterval` and `BatchSize`.

### 2. Domain & Persistence: Durable Work Items
Instead of scanning `WorkflowInstances` for anything runnable, the engine will transactionally enqueue explicit durable work items.

#### [NEW] [WorkItem.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Domain/Entities/WorkItem.cs)
- Introduce the `WorkItem` entity representing asynchronous intent.
- **Properties**: `Id`, `TenantId`, `WorkflowInstanceId`, `WorkType` (ExecuteStep, RetryStep, ResumeWorkflow, ProcessDeadline, DispatchEvent), `Status` (Pending, Processing, Completed, DeadLettered), `AvailableAt`, `AttemptCount`, `LockedBy`, `LockedUntil`, `LastError`, `CreatedAt`, `CompletedAt`.

#### [NEW] [IWorkItemRepository.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Application/Interfaces/IWorkItemRepository.cs)
- Interface for creating, polling, claiming (atomic lock), and completing work items.

#### [NEW] [EfCoreWorkItemRepository.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.EntityFramework/Repositories/EfCoreWorkItemRepository.cs)
- Implements `IWorkItemRepository`.
- **Safe Claiming**: Uses EF Core to perform an atomic UPDATE that sets `Status = Processing`, `LockedBy = [workerId]`, and `LockedUntil = [UtcNow + leaseDuration]` where `Status = Pending` and `AvailableAt <= UtcNow`.
- **Crash Recovery**: Allows reclaiming items where `LockedUntil < UtcNow`.

#### [MODIFY] [AroraWorkflowDbContext / Mappings]
- Add `DbSet<WorkItem>` (or mapping configuration) for `aw_workflow_work_items`.
- Generate EF Core migrations for the new table.

### 3. Core Engine Adjustments
The core engine remains passive but must now generate `WorkItem` records when asynchronous work is required.

#### [MODIFY] [WorkflowEngine.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Internal/Engine/WorkflowEngine.cs)
- When a workflow is started or transitions, the engine will evaluate if the next step is asynchronous or delayed. If so, it will create a `WorkItem` rather than executing it inline.
- **Rule**: `await workflowService.StartAsync(...)` remains synchronous up to persistence. It will persist the new state and the `WorkItem`, committing the transaction, and then return. The background worker takes over from there.

#### [MODIFY] Retry Policy Handling
- If a step fails transiently, a `RetryStep` work item is created with a future `AvailableAt` date.
- Permanent failures or max-retries-exceeded push the item to `DeadLettered`.

### 4. Tenancy & Observability

#### Tenancy Isolation
- The worker will extract `TenantId` directly from the `WorkItem` and set up an isolated scope (using `ITenantContext` if it exists, or via scoped dependencies) before executing the work.

#### Observability
- Add structured logging throughout the background processor capturing `WorkflowInstanceId`, `WorkItemId`, `TenantId`, and correlation IDs.
- Log metrics: items claimed, completed, retried, dead-lettered, processing duration, queue depth, and oldest pending item age.

## Verification Plan

### Automated Tests
- **Unit Tests**:
  - Verify `WorkItem` state transitions.
  - Verify that `WorkflowEngine` generates `WorkItem` records transactionally when required.
- **Integration Tests**:
  - Test the `EfCoreWorkItemRepository` for atomic claiming under concurrent load.
  - Test `WorkflowBackgroundService` end-to-end: start a worker, ensure it picks up a pending work item, executes it, and marks it completed.
  - Test crash recovery (simulating an expired lease).
  - Test graceful shutdown behavior.
