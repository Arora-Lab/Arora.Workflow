import React from 'react';
export interface InstanceListProps {
    onSelectInstance?: (instanceId: string) => void;
    selectedInstanceId?: string;
    definitionIdFilter?: string;
}
export declare const InstanceList: React.FC<InstanceListProps>;
