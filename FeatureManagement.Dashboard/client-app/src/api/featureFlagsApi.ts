import axios from 'axios';
import { FeatureFlag, FeatureFlagAuditLog, FeatureFlagActivityEntry } from '../types/featureFlags';

const API_BASE = '/api/feature-flags';

export const featureFlagsApi = {
    getAll: async (): Promise<FeatureFlag[]> => {
        const response = await axios.get<FeatureFlag[]>(API_BASE);
        return response.data;
    },
    getByName: async (name: string): Promise<FeatureFlag> => {
        const response = await axios.get<FeatureFlag>(`${API_BASE}/${name}`);
        return response.data;
    },
    create: async (flag: FeatureFlag): Promise<FeatureFlag> => {
        const response = await axios.post<FeatureFlag>(API_BASE, flag);
        return response.data;
    },
    update: async (name: string, flag: FeatureFlag, ifMatchVersion?: number): Promise<void> => {
        const config = ifMatchVersion === undefined
            ? undefined
            : { headers: { 'If-Match': `"v${ifMatchVersion}"` } };
        await axios.put(`${API_BASE}/${name}`, flag, config);
    },
    getAudit: async (name: string): Promise<FeatureFlagAuditLog[]> => {
        const response = await axios.get<FeatureFlagAuditLog[]>(`${API_BASE}/${name}/audit`);
        return response.data;
    },
    rollback: async (name: string, targetVersion: number): Promise<FeatureFlag> => {
        const response = await axios.post<FeatureFlag>(`${API_BASE}/${name}/rollback/${targetVersion}`);
        return response.data;
    },
    delete: async (name: string): Promise<void> => {
        await axios.delete(`${API_BASE}/${name}`);
    },
    getActivity: async (name: string): Promise<FeatureFlagActivityEntry[]> => {
        const response = await axios.get<FeatureFlagActivityEntry[]>(`${API_BASE}/${name}/activity`);
        return response.data;
    },
    scheduleChange: async (name: string, flag: FeatureFlag, scheduledAtUtc: string): Promise<FeatureFlag> => {
        const response = await axios.post<FeatureFlag>(`${API_BASE}/${name}/schedule`, {
            flag,
            scheduledAtUtc
        });
        return response.data;
    }
};