import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import { featureFlagsApi } from './featureFlagsApi';
import { FeatureFlag } from '../types/featureFlags';

vi.mock('axios', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn()
  }
}));

const mockedAxios = vi.mocked(axios);

const sampleFlags: FeatureFlag[] = [
  { name: 'alpha', requirementType: 0, enabledFor: [] },
  { name: 'beta', requirementType: 1, enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }] }
];

describe('featureFlagsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getAll returns feature flags from API response', async () => {
    mockedAxios.get.mockResolvedValue({ data: sampleFlags });

    const result = await featureFlagsApi.getAll();

    expect(mockedAxios.get).toHaveBeenCalledWith('/api/feature-flags');
    expect(result).toEqual(sampleFlags);
  });

  it('create posts flag and returns created value', async () => {
    const payload: FeatureFlag = { name: 'new-flag', requirementType: 0, enabledFor: [] };
    mockedAxios.post.mockResolvedValue({ data: payload });

    const result = await featureFlagsApi.create(payload);

    expect(mockedAxios.post).toHaveBeenCalledWith('/api/feature-flags', payload);
    expect(result).toEqual(payload);
  });

  it('update sends put request with route parameter', async () => {
    const payload: FeatureFlag = { name: 'beta', requirementType: 1, enabledFor: [] };
    mockedAxios.put.mockResolvedValue({});

    await featureFlagsApi.update('beta', payload);

    expect(mockedAxios.put).toHaveBeenCalledWith('/api/feature-flags/beta', payload);
  });
});