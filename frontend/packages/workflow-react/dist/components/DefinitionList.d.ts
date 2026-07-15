import React from 'react';
export interface DefinitionListProps {
    onSelectDefinition?: (definitionId: string) => void;
    selectedDefinitionId?: string;
}
export declare const DefinitionList: React.FC<DefinitionListProps>;
