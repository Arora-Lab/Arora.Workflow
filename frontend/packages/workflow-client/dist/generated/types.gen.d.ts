export type ClientOptions = {
    baseUrl: `${string}://swagger.json` | (string & {});
};
export type ConnectionLayout = {
    fromNode?: string | null;
    toNode?: string | null;
    condition?: string | null;
    points?: Array<LayoutPoint> | null;
};
export type DiagnosticSeverity = 0 | 1 | 2 | 3;
export type InvoiceRequest = {
    invoiceId?: string | null;
    amount?: number;
    vendorId?: string | null;
};
export type LayoutPoint = {
    x?: number;
    y?: number;
};
export type NodeLayout = {
    name?: string | null;
    type?: string | null;
    x?: number;
    y?: number;
    width?: number;
    height?: number;
};
export type WorkflowDefinitionDetails = {
    id?: string;
    name?: string | null;
    version?: number;
    description?: string | null;
    definitionJson?: string | null;
    createdAt?: string;
    layout?: WorkflowLayout;
    mermaid?: string | null;
    diagnostics?: Array<WorkflowDiagnostic> | null;
};
export type WorkflowDefinitionSummary = {
    id?: string;
    name?: string | null;
    version?: number;
    stepCount?: number;
    createdAt?: string;
};
export type WorkflowDefinitionSummaryPagedResult = {
    items?: Array<WorkflowDefinitionSummary> | null;
    totalCount?: number;
    page?: number;
    pageSize?: number;
};
export type WorkflowDiagnostic = {
    code?: string | null;
    severity?: DiagnosticSeverity;
    message?: string | null;
    nodeName?: string | null;
    suggestion?: string | null;
};
export type WorkflowHistoryItem = {
    id?: string;
    instanceId?: string;
    stepName?: string | null;
    action?: string | null;
    timestamp?: string;
    actor?: string | null;
    sequence?: number;
    nodeId?: string | null;
    fromState?: string | null;
    toState?: string | null;
    comment?: string | null;
};
export type WorkflowHistoryItemPagedResult = {
    items?: Array<WorkflowHistoryItem> | null;
    totalCount?: number;
    page?: number;
    pageSize?: number;
};
export type WorkflowInstanceDetails = {
    id?: string;
    workflowDefinitionId?: string;
    workflowDefinitionVersion?: number;
    status?: string | null;
    currentState?: string | null;
    inputJson?: string | null;
    createdAt?: string;
    modifiedAt?: string;
};
export type WorkflowInstanceSummary = {
    id?: string;
    workflowDefinitionId?: string;
    workflowDefinitionVersion?: number;
    status?: string | null;
    currentState?: string | null;
    createdAt?: string;
    modifiedAt?: string;
};
export type WorkflowInstanceSummaryPagedResult = {
    items?: Array<WorkflowInstanceSummary> | null;
    totalCount?: number;
    page?: number;
    pageSize?: number;
};
export type WorkflowLayout = {
    nodes?: Array<NodeLayout> | null;
    connections?: Array<ConnectionLayout> | null;
};
export type PostApiInvoicesData = {
    body: InvoiceRequest;
    path?: never;
    query?: never;
    url: '/api/invoices';
};
export type PostApiInvoicesResponses = {
    /**
     * OK
     */
    200: unknown;
};
export type GetApiInvoicesByIdStatusData = {
    body?: never;
    path: {
        id: string;
    };
    query?: never;
    url: '/api/invoices/{id}/status';
};
export type GetApiInvoicesByIdStatusResponses = {
    /**
     * OK
     */
    200: unknown;
};
export type GetApiInvoicesByIdHistoryData = {
    body?: never;
    path: {
        id: string;
    };
    query?: never;
    url: '/api/invoices/{id}/history';
};
export type GetApiInvoicesByIdHistoryResponses = {
    /**
     * OK
     */
    200: unknown;
};
export type GetApiApprovalsPendingByUserData = {
    body?: never;
    path: {
        user: string;
    };
    query?: never;
    url: '/api/approvals/pending/{user}';
};
export type GetApiApprovalsPendingByUserResponses = {
    /**
     * OK
     */
    200: unknown;
};
export type PostApiApprovalsByIdApproveData = {
    body?: never;
    path: {
        id: string;
    };
    query?: never;
    url: '/api/approvals/{id}/approve';
};
export type PostApiApprovalsByIdApproveResponses = {
    /**
     * OK
     */
    200: unknown;
};
export type PostApiApprovalsByIdRejectData = {
    body?: never;
    path: {
        id: string;
    };
    query?: never;
    url: '/api/approvals/{id}/reject';
};
export type PostApiApprovalsByIdRejectResponses = {
    /**
     * OK
     */
    200: unknown;
};
export type ListWorkflowDefinitionsData = {
    body?: never;
    path?: never;
    query?: {
        Page?: number;
        PageSize?: number;
    };
    url: '/arora/api/v1/definitions';
};
export type ListWorkflowDefinitionsResponses = {
    /**
     * OK
     */
    200: WorkflowDefinitionSummaryPagedResult;
};
export type ListWorkflowDefinitionsResponse = ListWorkflowDefinitionsResponses[keyof ListWorkflowDefinitionsResponses];
export type GetWorkflowDefinitionDetailsData = {
    body?: never;
    path: {
        id: string;
    };
    query?: never;
    url: '/arora/api/v1/definitions/{id}';
};
export type GetWorkflowDefinitionDetailsErrors = {
    /**
     * Not Found
     */
    404: unknown;
};
export type GetWorkflowDefinitionDetailsResponses = {
    /**
     * OK
     */
    200: WorkflowDefinitionDetails;
};
export type GetWorkflowDefinitionDetailsResponse = GetWorkflowDefinitionDetailsResponses[keyof GetWorkflowDefinitionDetailsResponses];
export type ListWorkflowInstancesData = {
    body?: never;
    path?: never;
    query?: {
        DefinitionId?: string;
        Status?: string;
        Page?: number;
        PageSize?: number;
    };
    url: '/arora/api/v1/instances';
};
export type ListWorkflowInstancesResponses = {
    /**
     * OK
     */
    200: WorkflowInstanceSummaryPagedResult;
};
export type ListWorkflowInstancesResponse = ListWorkflowInstancesResponses[keyof ListWorkflowInstancesResponses];
export type GetWorkflowInstanceData = {
    body?: never;
    path: {
        id: string;
    };
    query?: never;
    url: '/arora/api/v1/instances/{id}';
};
export type GetWorkflowInstanceErrors = {
    /**
     * Not Found
     */
    404: unknown;
};
export type GetWorkflowInstanceResponses = {
    /**
     * OK
     */
    200: WorkflowInstanceDetails;
};
export type GetWorkflowInstanceResponse = GetWorkflowInstanceResponses[keyof GetWorkflowInstanceResponses];
export type GetWorkflowInstanceHistoryData = {
    body?: never;
    path: {
        id: string;
    };
    query: {
        page: number;
        pageSize: number;
    };
    url: '/arora/api/v1/instances/{id}/history';
};
export type GetWorkflowInstanceHistoryResponses = {
    /**
     * OK
     */
    200: WorkflowHistoryItemPagedResult;
};
export type GetWorkflowInstanceHistoryResponse = GetWorkflowInstanceHistoryResponses[keyof GetWorkflowInstanceHistoryResponses];
