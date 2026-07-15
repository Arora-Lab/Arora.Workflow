# Phase 12: Workflow Visualizer & Developer Experience (DX)

The objective of Phase 12 is to build the core visualization, diagnostic, and command-line foundations for Arora Workflow, focusing purely on developer experience (DX). 

We will design these features as reusable **capabilities** within the C# core rather than UI-bound features, decoupling layout calculation and visualization formats from the React library.

---

## User Review Required

> [!IMPORTANT]
> **Decoupled SVG Layout Architecture**
> Graph layout computation will be abstracted behind a C# service interface `IWorkflowLayoutEngine`. The backend API will calculate node coordinates ($X, Y$) and edge paths.
> The `@arora/workflow-react` package will simply retrieve this layout and render it inside lightweight, responsive SVG viewports, allowing custom CSS styling and click handlers.
>
> **Diagnostics & Mermaid in Core**
> Mermaid diagram generation and the Roslyn-style `WorkflowDiagnosticsEngine` will reside in `Arora.Workflow` core, allowing them to be shared between the API endpoints, the new CLI tool, and future editor extensions.

---

## Proposed Changes

### 1. SDK Core: Layout, Diagnostics, & Export (`Arora.Workflow`)

We will build the foundational diagnostics, layout engines, and export utilities.

#### [NEW] [IWorkflowLayoutEngine.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Tooling/Layout/IWorkflowLayoutEngine.cs)
- Defines the layout engine contract:
  ```csharp
  public interface IWorkflowLayoutEngine
  {
      WorkflowLayout ComputeLayout(WorkflowGraph graph);
  }
  ```

#### [NEW] [LayoutModels.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Tooling/Layout/LayoutModels.cs)
- Declares the structured coordinates layout result:
  ```csharp
  public record NodeLayout(string Name, string Type, double X, double Y, double Width, double Height);
  public record ConnectionLayout(string FromNode, string ToNode, string? Condition, List<LayoutPoint> Points);
  public record LayoutPoint(double X, double Y);
  public record WorkflowLayout(List<NodeLayout> Nodes, List<ConnectionLayout> Connections);
  ```

#### [NEW] [LayeredLayoutEngine.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Tooling/Layout/LayeredLayoutEngine.cs)
- Default implementation of `IWorkflowLayoutEngine` using a BFS level-layering layout:
  - Groups nodes by hierarchy depth.
  - Spreads layers vertically ($Y$) and nodes horizontally ($X$).
  - Calculates link paths and sets control points for connection arrows.

#### [NEW] [WorkflowDiagnosticsEngine.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Tooling/Diagnostics/WorkflowDiagnosticsEngine.cs)
- Roslyn-style diagnostic engine parsing the `WorkflowGraph`.
- Checks rules:
  - `AW_001_CYCLES`: Cycle detection (DFS back-edge checker).
  - `AW_002_ORPHANS`: Reachability from initial node.
  - `AW_003_DEAD_ENDS`: Steps without exits that are not terminal states.
  - `AW_004_DUPLICATE_TRANSITIONS`: Double paths on identical triggers.
  - `AW_005_RESERVED_NAMES`: Warns if custom nodes use names reserved by the engine.
- Returns list of `WorkflowDiagnostic` records (ID, Severity, Message, Suggestion).

#### [NEW] [MermaidExporter.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow/Tooling/Export/MermaidExporter.cs)
- Generates Mermaid syntax from a `WorkflowGraph` (supports both Flowchart `graph TD` and Sequence Diagrams).

### 2. Management API & Server Additions (`Arora.Workflow.Management` & `AspNetCore`)

We will extend the query services and register endpoints to expose the new visualizer data.

#### [MODIFY] [IWorkflowQueryService.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.Management/IWorkflowQueryService.cs)
- Add `GetDefinitionDetailsAsync(Guid definitionId)` returning definition metadata, diagnostics, layout coordinates, and Mermaid representations.

#### [MODIFY] [EfCoreWorkflowQueryService.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.EntityFramework/Queries/EfCoreWorkflowQueryService.cs)
- Implement `GetDefinitionDetailsAsync` by retrieving definition JSON, running layout engine computation, and returning diagnostic reports.

#### [MODIFY] [AroraWorkflowApiExtensions.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.AspNetCore/AroraWorkflowApiExtensions.cs)
- Map new endpoint:
  - `GET /arora/api/v1/definitions/{id}` - Returns full diagnostic and layout details.

### 3. Frontend React Visualizer (`@arora/workflow-react`)

We will update the hooks and add pure-SVG render components.

#### [NEW] [useWorkflowDefinitionDetails.ts](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/frontend/packages/workflow-react/src/hooks/useWorkflowDefinitionDetails.ts)
- Fetches full definition metadata and computed layout from `/arora/api/v1/definitions/{id}`.

#### [NEW] [WorkflowVisualizer.tsx](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/frontend/packages/workflow-react/src/components/WorkflowVisualizer.tsx)
- Renders computed coordinates inside a clean, scalable `<svg>` canvas.
- Features:
  - Hover highlights and click selection events.
  - Custom zoom & pan handlers.
  - Styling tokens mapping layout node colors (e.g. running state, completed nodes, pending approvals).

#### [MODIFY] [InstanceDetailsView.tsx](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/frontend/packages/workflow-react/src/components/InstanceDetailsView.tsx)
- Render the `WorkflowVisualizer` directly above the instance properties panel.
- Highlights the `currentState` node dynamically.

#### [MODIFY] [WorkflowDashboard.tsx](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/frontend/packages/workflow-react/src/components/WorkflowDashboard.tsx)
- Add new tab panels:
  - "Playground": Displays playground simulator.
  - "Analytics": Renders metrics counts and averages.
- Integrate `WorkflowVisualizer` and `TimeTravelDebugger` directly within the Instance Details sidebar/drawer.

### 4. CLI Scaffolding (`Arora.Workflow.Cli`)

We will build a global CLI tool that integrates with build/CI pipelines.

#### [NEW] [Arora.Workflow.Cli.csproj](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.Cli/Arora.Workflow.Cli.csproj)
- Console project compiling into `arora` command tool.

#### [NEW] [Program.cs](file:///C:/Users/Brian/Documents/Develop/Arora%20Workflow/src/Arora.Workflow.Cli/Program.cs)
- Entrypoint parsing commands (using `System.CommandLine`):
  - `arora workflow lint <path-to-assembly>`: Runs validation lint rules, outputs formatting list, fails build if errors occur.
  - `arora workflow export <path-to-assembly> --format mermaid`: Outputs Mermaid markdown.
  - `arora workflow diff <v1-assembly> <v2-assembly>`: Displays transition diff changes between versions.

---

## Verification Plan

### Automated Tests
- Build C# backend and React workspaces.
- Write unit tests for `WorkflowDiagnosticsEngine` validating loop detection and orphan warnings.
- Test `LayeredLayoutEngine` verifying node coordinate bounds.

### Manual Verification
- Run the CLI tool `arora workflow lint` against mock assemblies to test detection.
- Open the visualizer in the React dashboard, verifying layout scaling and viewport pan gestures.
