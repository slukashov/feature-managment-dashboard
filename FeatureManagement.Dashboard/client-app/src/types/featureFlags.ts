export interface FeatureFilter {
    name: string;
    parametersJson: string;
}

export interface FeatureFlag {
    name: string;
    owner: string;
    tags: string[];
    requirementType: number; // 0 = Any, 1 = All
    enabledFor: FeatureFilter[];
    version?: number;
    updatedAtUtc?: string;
    scheduledAtUtc?: string;
}

export interface FeatureFlagAuditLog {
    id: number;
    featureFlagName: string;
    action: number;
    snapshotVersion: number;
    snapshotJson: string;
    changedAtUtc: string;
    changedBy: string;
}

export interface FeatureFlagActivityEntry {
    id: number;
    featureFlagName: string;
    activityType: string;
    description: string;
    changeType?: string;
    changedAtUtc: string;
    changedBy: string;
}


export type NotificationSeverity = 'success' | 'error' | 'info' | 'warning';