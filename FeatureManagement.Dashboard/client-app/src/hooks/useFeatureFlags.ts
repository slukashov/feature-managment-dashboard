import { useState, useEffect, useCallback } from 'react';
import { featureFlagsApi } from '../api/featureFlagsApi';
import { FeatureFlag, NotificationSeverity } from '../types/featureFlags';

type ShowNotificationFn = (message: string, severity: NotificationSeverity) => void;

export const useFeatureFlags = (showNotification: ShowNotificationFn) => {
    const [flags, setFlags] = useState<FeatureFlag[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const fetchFlags = useCallback(async () => {
        setIsLoading(true);
        try {
            const data = await featureFlagsApi.getAll();
            setFlags(data);
        } catch (error) {
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
        } catch (error) {
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
        } catch (error) {
            showNotification("Failed to toggle feature flag.", "error");
        }
    };

    return { flags, isLoading, fetchFlags, saveFlag, toggleFlag };
};