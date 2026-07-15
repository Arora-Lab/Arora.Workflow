# Phase 13: Interactive Debugging

The objective of Phase 13 is to build visual execution debugging and time-travel path analysis. When a developer inspects a workflow instance, they can scrub through the history timeline to see exactly how the state moved on the SVG visualizer, including the input parameter snapshots, actors, and comment logs at each transition point.

---

## User Review Required

> [!IMPORTANT]
> **API Schema Modifications**
> To support timeline scrubbing and transition comments, we will extend the `WorkflowHistoryItem` payload returned by `GET /instances/{id}/history` to include:
> - `FromState` (string)
> - `ToState` (string)
> - `Comment` (string)
>
> These fields are already persisted in the database via `WorkflowHistoryEntity` (EF Core) but are currently omitted when querying from the query service.

---

## Proposed Changes

### 1. Management API Extensions (C# Backend)

We will modify the query models and database projections.

#### [MODIFY] WorkflowHistoryItem.cs
- Extend the `WorkflowHistoryItem` record:
  ```csharp
  public record WorkflowHistoryItem(
      Guid Id,
      Guid InstanceId,
      string? StepName,
      string Action,
      DateTimeOffset Timestamp,
      string? Actor,
      string? FromState,
      string? ToState,
      string? Comment);
  ```

#### [MODIFY] EfCoreWorkflowQueryService.cs
- Update the LINQ projection inside `GetInstanceHistoryAsync` to fetch `FromState`, `ToState`, and `Comment` from `WorkflowHistoryEntity`.

### 2. Frontend React Additions (`@arora/workflow-react`)

We will update the timeline selection handlers and connect visual state scrubbing.

#### [MODIFY] HistoryTimeline.tsx
- Add a callback prop `onSelectHistoryItem?: (item: WorkflowHistoryItem | null) => void`.
- Hold state for `selectedHistoryItemId` and apply highlighting styles to the selected timeline event.
- Render details of the clicked history event (e.g. show user comment box, step name, and timestamp properties).

#### [MODIFY] WorkflowDashboard.tsx
- Manage shared state for the active history transition item `selectedHistoryItem`.
- Clear `selectedHistoryItem` when switching instances.

#### [MODIFY] InstanceDetailsView.tsx
- Connect `selectedHistoryItem` into the SVG visualizer:
  - If a historical item is selected, override `activeNodeName` inside the visualizer to highlight the clicked item's `ToState` (or `StepName`) instead of the current running state.
  - Display a "Variable & Comment Inspector" side panel showing details of the selected transition event.

---

## Verification Plan

### Automated Tests
- Regenerate the TypeScript client matching the new API fields.
- Build the solution (`dotnet build` and `npm run build`).

### Manual Verification
- Start the sample application and launch a workflow instance.
- Click through various steps in the instance history timeline.
- Verify that the SVG visualizer updates its highlighted node to match the selected history step.
- Verify that the comment, actor, and state info panels update dynamically.
