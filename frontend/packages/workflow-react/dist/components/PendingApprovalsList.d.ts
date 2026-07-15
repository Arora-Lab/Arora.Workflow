import React from 'react';
export interface PendingApprovalsListProps {
    onSelectInstance?: (instanceId: string) => void;
}
export declare const PendingApprovalsList: React.FC<PendingApprovalsListProps>;
