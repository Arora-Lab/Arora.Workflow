import type { Client, ClientMeta, Options as Options2, RequestResult, TDataShape } from './client';
import type { GetApiApprovalsPendingByUserData, GetApiApprovalsPendingByUserResponses, GetApiInvoicesByIdHistoryData, GetApiInvoicesByIdHistoryResponses, GetApiInvoicesByIdStatusData, GetApiInvoicesByIdStatusResponses, GetWorkflowInstanceData, GetWorkflowInstanceErrors, GetWorkflowInstanceHistoryData, GetWorkflowInstanceHistoryResponses, GetWorkflowInstanceResponses, ListWorkflowDefinitionsData, ListWorkflowDefinitionsResponses, ListWorkflowInstancesData, ListWorkflowInstancesResponses, PostApiApprovalsByIdApproveData, PostApiApprovalsByIdApproveResponses, PostApiApprovalsByIdRejectData, PostApiApprovalsByIdRejectResponses, PostApiInvoicesData, PostApiInvoicesResponses } from './types.gen';
export type Options<TData extends TDataShape = TDataShape, ThrowOnError extends boolean = boolean, TResponse = unknown> = Options2<TData, ThrowOnError, TResponse> & {
    /**
     * You can provide a client instance returned by `createClient()` instead of
     * individual options. This might be also useful if you want to implement a
     * custom client.
     */
    client?: Client;
    /**
     * You can pass arbitrary values through the `meta` object. This can be
     * used to access values that aren't defined as part of the SDK function.
     */
    meta?: keyof ClientMeta extends never ? Record<string, unknown> : ClientMeta;
};
export declare const postApiInvoices: <ThrowOnError extends boolean = false>(options: Options<PostApiInvoicesData, ThrowOnError>) => RequestResult<PostApiInvoicesResponses, unknown, ThrowOnError>;
export declare const getApiInvoicesByIdStatus: <ThrowOnError extends boolean = false>(options: Options<GetApiInvoicesByIdStatusData, ThrowOnError>) => RequestResult<GetApiInvoicesByIdStatusResponses, unknown, ThrowOnError>;
export declare const getApiInvoicesByIdHistory: <ThrowOnError extends boolean = false>(options: Options<GetApiInvoicesByIdHistoryData, ThrowOnError>) => RequestResult<GetApiInvoicesByIdHistoryResponses, unknown, ThrowOnError>;
export declare const getApiApprovalsPendingByUser: <ThrowOnError extends boolean = false>(options: Options<GetApiApprovalsPendingByUserData, ThrowOnError>) => RequestResult<GetApiApprovalsPendingByUserResponses, unknown, ThrowOnError>;
export declare const postApiApprovalsByIdApprove: <ThrowOnError extends boolean = false>(options: Options<PostApiApprovalsByIdApproveData, ThrowOnError>) => RequestResult<PostApiApprovalsByIdApproveResponses, unknown, ThrowOnError>;
export declare const postApiApprovalsByIdReject: <ThrowOnError extends boolean = false>(options: Options<PostApiApprovalsByIdRejectData, ThrowOnError>) => RequestResult<PostApiApprovalsByIdRejectResponses, unknown, ThrowOnError>;
export declare const listWorkflowDefinitions: <ThrowOnError extends boolean = false>(options?: Options<ListWorkflowDefinitionsData, ThrowOnError>) => RequestResult<ListWorkflowDefinitionsResponses, unknown, ThrowOnError>;
export declare const listWorkflowInstances: <ThrowOnError extends boolean = false>(options?: Options<ListWorkflowInstancesData, ThrowOnError>) => RequestResult<ListWorkflowInstancesResponses, unknown, ThrowOnError>;
export declare const getWorkflowInstance: <ThrowOnError extends boolean = false>(options: Options<GetWorkflowInstanceData, ThrowOnError>) => RequestResult<GetWorkflowInstanceResponses, GetWorkflowInstanceErrors, ThrowOnError>;
export declare const getWorkflowInstanceHistory: <ThrowOnError extends boolean = false>(options: Options<GetWorkflowInstanceHistoryData, ThrowOnError>) => RequestResult<GetWorkflowInstanceHistoryResponses, unknown, ThrowOnError>;
