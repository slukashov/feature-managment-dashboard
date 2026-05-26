import { useState, useEffect, useCallback } from 'react';
import { featureFlagsApi } from '../api/featureFlagsApi';
import { FeatureFlag, FeatureFlagAuditLog, FeatureFlagActivityEntry, NotificationSeverity } from '../types/featureFlags';

type ShowNotificationFn = (message: string, severity: NotificationSeverity) => void;

const logUnexpectedError = (context: string, error: unknown): void => {
    // Keep UI messages simple while preserving debugging details.
    console.error(`[useFeatureFlags] ${context}`, error);
};

export const useFeatureFlags = (showNotification: ShowNotificationFn) => {
    const [flags, setFlags] = useState<FeatureFlag[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const normalizeFlag = (flag: FeatureFlag): FeatureFlag => ({
        ...flag,
        owner: flag.owner ?? '',
        tags: Array.isArray(flag.tags) ? flag.tags : []
    });

    const fetchFlags = useCallback(async () => {
        setIsLoading(true);
        try {
            const data = await featureFlagsApi.getAll();
            setFlags(data.map(normalizeFlag));
        } catch (error: unknown) {
            logUnexpectedError('fetchFlags failed', error);
            showNotification("Failed to load feature flags.", "error");
        } finally {
            setIsLoading(false);
        }
    }, [showNotification]);

    useEffect(() => {
        fetchFlags();
    }, [fetchFlags]);

    const saveFlag = async (flag: FeatureFlag, isNew: boolean): Promise<boolean> => {
        try {
            if (isNew) {
                await featureFlagsApi.create(flag);
            } else {
                await featureFlagsApi.update(flag.name, flag);
            }
            showNotification(`Feature ${isNew ? 'created' : 'updated'} successfully!`, "success");
            await fetchFlags();
            return true;
        } catch (error: unknown) {
            logUnexpectedError('saveFlag failed', error);
            showNotification("Failed to save feature flag.", "error");
            return false;
        }
    };

    const toggleFlag = async (row: FeatureFlag): Promise<void> => {
        const updated: FeatureFlag = {
            ...row,
            enabledFor: row.enabledFor.length > 0 ? [] : [{ name: 'AlwaysOn', parametersJson: '{}' }]
        };
        try {
            await featureFlagsApi.update(row.name, updated);
            showNotification(`${row.name} turned ${updated.enabledFor.length > 0 ? 'ON' : 'OFF'}`, "success");
            await fetchFlags();
        } catch (error: unknown) {
            logUnexpectedError('toggleFlag failed', error);
            showNotification("Failed to toggle feature flag.", "error");
        }
    };

    const getAuditHistory = async (name: string): Promise<FeatureFlagAuditLog[] | null> => {
        try {
            return await featureFlagsApi.getAudit(name);
        } catch (error: unknown) {
            logUnexpectedError('getAuditHistory failed', error);
            showNotification('Failed to load feature history.', 'error');
            return null;
        }
    };

    const rollbackFlag = async (name: string, targetVersion: number): Promise<boolean> => {
        try {
            await featureFlagsApi.rollback(name, targetVersion);
            showNotification(`${name} rolled back to version ${targetVersion}.`, 'success');
            await fetchFlags();
            return true;
        } catch (error: unknown) {
            logUnexpectedError('rollbackFlag failed', error);
            showNotification('Failed to rollback feature flag.', 'error');
            return false;
        }
    };

    const deleteFlag = async (name: string): Promise<boolean> => {
        try {
            await featureFlagsApi.delete(name);
            showNotification(`Feature ${name} deleted successfully!`, 'success');
            await fetchFlags();
            return true;
        } catch (error: unknown) {
            logUnexpectedError('deleteFlag failed', error);
            showNotification('Failed to delete feature flag.', 'error');
            return false;
        }
    };

    const getActivityFeed = async (name: string): Promise<FeatureFlagActivityEntry[] | null> => {
        try {
            return await featureFlagsApi.getActivity(name);
        } catch (error: unknown) {
            logUnexpectedError('getActivityFeed failed', error);
            showNotification('Failed to load activity feed.', 'error');
            return null;
        }
    };

    const scheduleChange = async (name: string, flag: FeatureFlag, scheduledAtUtc: string): Promise<boolean> => {
        try {
            await featureFlagsApi.scheduleChange(name, flag, scheduledAtUtc);
            showNotification(`${name} scheduled for ${new Date(scheduledAtUtc).toLocaleString()}.`, 'success');
            await fetchFlags();
            return true;
        } catch (error: unknown) {
            logUnexpectedError('scheduleChange failed', error);
            showNotification('Failed to schedule feature flag change.', 'error');
            return false;
        }
    };

    return { flags, isLoading, fetchFlags, saveFlag, toggleFlag, getAuditHistory, rollbackFlag, deleteFlag, getActivityFeed, scheduleChange };
};