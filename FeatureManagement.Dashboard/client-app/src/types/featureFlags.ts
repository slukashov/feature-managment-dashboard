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

export interface FeatureFlagExperimentConfiguration {
    baselineVariant: string;
    challengerVariant: string;
    baselineTrafficPercentage: number;
    challengerTrafficPercentage: number;
    conversionMetricName: string;
    latencyMetricName: string;
    minimumSampleSize: number;
    isActive: boolean;
}

export interface FeatureFlagExperimentOutcome {
    variant: string;
    converted: boolean;
    hasError: boolean;
    latencyMs: number;
}

export interface FeatureFlagExperimentVariantAssignment {
    variant: string;
    bucket: number;
}

export interface FeatureFlagExperimentVariantSnapshot {
    variant: string;
    sampleSize: number;
    conversionCount: number;
    errorCount: number;
    conversionRate: number;
    errorRate: number;
    averageLatencyMs: number;
    score: number;
}

export interface FeatureFlagExperimentRecommendation {
    status: number;
    recommendedVariant?: string;
    reason: string;
    baseline: FeatureFlagExperimentVariantSnapshot;
    challenger: FeatureFlagExperimentVariantSnapshot;
}


export type NotificationSeverity = 'success' | 'error' | 'info' | 'warning';