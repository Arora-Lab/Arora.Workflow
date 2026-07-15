# Phase 11: React UI Components (@arora/workflow-react)

The objective of Phase 11 is to build a modern, premium React component library (`@arora/workflow-react`) in Vanilla CSS that allows developers to monitor, inspect, and approve/reject workflows directly within a React web application.

The package will leverage `@arora/workflow-client` to interact with the Arora Workflow Management API and provide pre-built, high-fidelity components (Timeline, Instance Lists, Custom Hooks, and a full-featured Workflow Dashboard).

## Proposed Changes

### 1. Scaffolding `@arora/workflow-react`

We will create a new package inside `frontend/packages/workflow-react` with TypeScript, React, and build configurations.

#### [NEW] package.json
- Declares the name `@arora/workflow-react`.
- Specifies exports for JS (`dist/index.js`) and type declarations (`dist/index.d.ts`).
- Defines peer dependencies for `react` and `react-dom` (supporting React 18 and 19).
- Adds `@arora/workflow-client` as a dependency (using workspace protocol).

#### [NEW] tsconfig.json
- Configures compiler options for React compiler (`"jsx": "react-jsx"`).
- Target `es2022`, module `commonjs` (consistent with `@arora/workflow-client`).
- Outputs declaration files and sets up the include paths.

### 2. State & Client Context

We will build a React Context that stores client instance settings and the current active user for executing approval actions.

#### [NEW] AroraWorkflowContext.tsx
- Exposes `AroraWorkflowProvider` and a `useAroraWorkflowContext` hook.
- Accepts `baseUrl?: string`, a custom `client?: Client` configuration, and `currentUser?: string` (the ID of the actor submitting approvals).

### 3. Custom Hooks

We will build reusable hooks that abstract the API client logic, pagination, and caching.

#### [NEW] useWorkflowDefinitions.ts
- Retrieves workflow definitions with pagination.
- Returns `{ definitions, totalCount, loading, error, refetch, page, setPage }`.

#### [NEW] useWorkflowInstances.ts
- Retrieves workflow instances with status filters, definition ID filter, and pagination.
- Returns `{ instances, totalCount, loading, error, refetch, page, setPage, statusFilter, setStatusFilter, definitionFilter, setDefinitionFilter }`.

#### [NEW] useWorkflowInstanceDetails.ts
- Retrieves details of a specific workflow instance.
- Returns `{ instance, loading, error, refetch }`.

#### [NEW] useWorkflowInstanceHistory.ts
- Retrieves the history timeline for a workflow instance.
- Returns `{ history, totalCount, loading, error, refetch, page, setPage }`.

#### [NEW] usePendingApprovals.ts
- Fetches all pending approvals assigned to the current user.
- Exposes `approve(approvalId, comment)` and `reject(approvalId, comment)` async callbacks to submit decisions.

### 4. UI Components & Premium Styling

We will design a cohesive, responsive UI library containing several core views. They will be styled using a single CSS stylesheet (`styles.css`) utilizing modern design patterns:
- Glassmorphism backgrounds, subtle drop shadows, and border colors.
- Micro-animations and hover transitions for interactive rows/cards.
- Color coding for statuses: **Running** (Indigo/Blue), **Completed** (Green/Emerald), **Rejected** (Red/Rose), **Cancelled** (Gray/Slate), **PendingApproval** (Amber/Purple).

#### [NEW] styles.css
- Contains all visual styles, variables, typography mapping, and icons.
- Exposes CSS variables like `--arora-primary`, `--arora-accent`, `--arora-font-sans`, `--arora-radius`, `--arora-border`, and variables for light/dark mode.

#### [NEW] DefinitionList.tsx
- Grid/table display of workflow definitions showing ID, Name, Version, Step Count, and Creation Date.

#### [NEW] InstanceList.tsx
- List of instances with search, dropdown status filters (e.g. Running, PendingApproval, etc.), definition selectors, and pagination controls.
- Displays key details: status badge, current state name, created time, and duration.

#### [NEW] InstanceDetailsView.tsx
- Displays details of an instance.
- Includes a code inspector panel (with copy to clipboard) to check `inputJson` formatting.
- Features quick actions (e.g. "Cancel Instance" or trigger approvals if applicable).

#### [NEW] HistoryTimeline.tsx
- A modern vertical timeline showing chronological workflow history (actions like Started, Transitioned, StepExecuted, ApprovalRequested, ApprovalGranted, Completed, etc.).
- Renders nice icons representing each step action type.

#### [NEW] PendingApprovalsList.tsx
- Card-based interface showing pending approvals assigned to the current user (e.g., invoice approvals, request forms).
- Includes text comments boxes and quick action buttons ("Approve", "Reject") with loading spinner states.

#### [NEW] WorkflowDashboard.tsx
- A unified dashboard wrapper that compiles these components into a double-pane or tabbed layout (e.g. tabs for Instances, Definitions, Pending Approvals, with full detail overlays).

#### [NEW] index.ts
- Entry point exporting all hooks, components, provider, and importing the global styles.

---

## Verification Plan

### Automated Tests
- Run `npm run build` inside `frontend/packages/workflow-react` to ensure compilation completes without TypeScript or React compiler errors.

### Manual Verification
- We can write a simple test wrapper or check that the package is correctly recognized as an NPM workspace.
- Validate that types export correctly and cover all endpoints of the `@arora/workflow-client`.
