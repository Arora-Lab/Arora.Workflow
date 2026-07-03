# Arora Workflow — Phase 4 Implementation Plan

The objective of Phase 4 is to finalize the Entity Framework Core integration for the `Approval` aggregate created in Phase 2, and to introduce the Background Worker capability so the engine can run autonomously in host applications.

## User Review Required
> [!IMPORTANT]
> The current plan suggests creating a new Background Worker service to poll for and execute pending workflows, turning Arora Workflow into an active background system rather than just a passive engine. Please confirm if this is the correct direction for Phase 4.

## Proposed Changes

### 1. Entity Framework Core Persistence Completion

#### [NEW] [EfCoreApprovalRepository.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.EntityFramework/Repositories/EfCoreApprovalRepository.cs)
- Implement `IApprovalRepository`.
- Map the EF Core `ApprovalEntity` data model to the rich `Approval` domain aggregate.
- Map the `Approval` domain aggregate back to the `ApprovalEntity` on saving.

#### [MODIFY] [AroraWorkflowEntityFrameworkExtensions.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.EntityFramework/AroraWorkflowEntityFrameworkExtensions.cs)
- Register `EfCoreApprovalRepository` into the Dependency Injection container.

### 2. Background Processing (Worker Service)

#### [NEW] [WorkflowBackgroundService.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Internal/Hosting/WorkflowBackgroundService.cs)
- Implement a `.NET IHostedService / BackgroundService` that runs continuously in the background of the host application.
- Periodically poll the repository for queued or running workflows, and invoke the `WorkflowEngine.AdvanceAsync()` method automatically.

#### [MODIFY] [AroraWorkflowBuilder.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Application/Interfaces/AroraWorkflowBuilder.cs)
- Add a `.AddBackgroundWorker()` extension to optionally register the background service.

## Verification Plan

### Automated Tests
- Write a unit test for `EfCoreApprovalRepository` using an InMemory database.
- Write a test for `WorkflowBackgroundService` using a mocked repository to ensure it correctly polls and triggers the engine.
