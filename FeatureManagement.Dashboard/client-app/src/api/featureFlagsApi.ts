import axios from 'axios';
import { FeatureFlag } from '../types/featureFlags';

const API_BASE = '/api/feature-flags';

export const featureFlagsApi = {
    getAll: async (): Promise<FeatureFlag[]> => {
        const response = await axios.get<FeatureFlag[]>(API_BASE);
        return response.data;
    },
    create: async (flag: FeatureFlag): Promise<FeatureFlag> => {
        const response = await axios.post<FeatureFlag>(API_BASE, flag);
        return response.data;
    },
    update: async (name: string, flag: FeatureFlag): Promise<void> => {
        await axios.put(`${API_BASE}/${name}`, flag);
    }
};