export interface FeatureFilter {
    name: string;
    parametersJson: string;
}

export interface FeatureFlag {
    name: string;
    requirementType: number; // 0 = Any, 1 = All
    enabledFor: FeatureFilter[];
    version?: number;
    updatedAtUtc?: string;
}


export type NotificationSeverity = 'success' | 'error' | 'info' | 'warning';