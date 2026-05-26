import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import { featureFlagsApi } from './featureFlagsApi';
import { FeatureFlag } from '../types/featureFlags';

vi.mock('axios', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  }
}));

const mockedAxios = vi.mocked(axios);

const sampleFlags: FeatureFlag[] = [
  { name: 'alpha', owner: 'team-a', tags: ['alpha'], requirementType: 0, enabledFor: [] },
  { name: 'beta', owner: 'team-b', tags: ['beta'], requirementType: 1, enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }] }
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
    const payload: FeatureFlag = { name: 'new-flag', owner: 'team-a', tags: ['new'], requirementType: 0, enabledFor: [] };
    mockedAxios.post.mockResolvedValue({ data: payload });

    const result = await featureFlagsApi.create(payload);

    expect(mockedAxios.post).toHaveBeenCalledWith('/api/feature-flags', payload);
    expect(result).toEqual(payload);
  });

  it('getByName returns feature flag by key', async () => {
    const payload: FeatureFlag = { name: 'beta', owner: 'team-b', tags: ['beta'], requirementType: 1, enabledFor: [] };
    mockedAxios.get.mockResolvedValue({ data: payload });

    const result = await featureFlagsApi.getByName('beta');

    expect(mockedAxios.get).toHaveBeenCalledWith('/api/feature-flags/beta');
    expect(result).toEqual(payload);
  });

  it('update sends put request with route parameter', async () => {
    const payload: FeatureFlag = { name: 'beta', owner: 'team-b', tags: ['beta'], requirementType: 1, enabledFor: [] };
    mockedAxios.put.mockResolvedValue({});

    await featureFlagsApi.update('beta', payload);

    expect(mockedAxios.put).toHaveBeenCalledWith('/api/feature-flags/beta', payload, undefined);
  });

  it('update sends If-Match header when version is provided', async () => {
    const payload: FeatureFlag = { name: 'beta', owner: 'team-b', tags: ['beta'], requirementType: 1, enabledFor: [] };
    mockedAxios.put.mockResolvedValue({});

    await featureFlagsApi.update('beta', payload, 4);

    expect(mockedAxios.put).toHaveBeenCalledWith('/api/feature-flags/beta', payload, {
      headers: {
        'If-Match': '"v4"'
      }
    });
  });

  it('getAudit returns audit entries for feature', async () => {
    const auditEntries = [
      {
        id: 10,
        featureFlagName: 'beta',
        action: 2,
        snapshotVersion: 2,
        snapshotJson: '{"name":"beta"}',
        changedAtUtc: '2026-05-26T10:00:00Z',
        changedBy: 'system'
      }
    ];
    mockedAxios.get.mockResolvedValue({ data: auditEntries });

    const result = await featureFlagsApi.getAudit('beta');

    expect(mockedAxios.get).toHaveBeenCalledWith('/api/feature-flags/beta/audit');
    expect(result).toEqual(auditEntries);
  });

  it('rollback posts target version and returns rolled back flag', async () => {
    const rolledBack: FeatureFlag = { name: 'beta', owner: 'team-b', tags: ['beta'], requirementType: 1, enabledFor: [], version: 3 };
    mockedAxios.post.mockResolvedValue({ data: rolledBack });

    const result = await featureFlagsApi.rollback('beta', 1);

    expect(mockedAxios.post).toHaveBeenCalledWith('/api/feature-flags/beta/rollback/1');
    expect(result).toEqual(rolledBack);
  });

  it('delete sends delete request for feature flag', async () => {
    mockedAxios.delete.mockResolvedValue({});

    await featureFlagsApi.delete('beta');

    expect(mockedAxios.delete).toHaveBeenCalledWith('/api/feature-flags/beta');
  });

  it('getActivity returns activity entries for feature', async () => {
    const activityEntries = [
      {
        id: 1,
        featureFlagName: 'beta',
        activityType: 'Updated',
        description: 'Enabled the feature',
        changeType: 'EnabledFor',
        changedAtUtc: '2026-05-26T10:00:00Z',
        changedBy: 'user@example.com'
      }
    ];
    mockedAxios.get.mockResolvedValue({ data: activityEntries });

    const result = await featureFlagsApi.getActivity('beta');

    expect(mockedAxios.get).toHaveBeenCalledWith('/api/feature-flags/beta/activity');
    expect(result).toEqual(activityEntries);
  });

  it('scheduleChange posts scheduled change request and returns flag', async () => {
    const scheduled: FeatureFlag = { name: 'beta', owner: 'team-b', tags: ['beta'], requirementType: 1, enabledFor: [], scheduledAtUtc: '2026-05-27T14:00:00Z' };
    mockedAxios.post.mockResolvedValue({ data: scheduled });

    const result = await featureFlagsApi.scheduleChange('beta', scheduled, '2026-05-27T14:00:00Z');

    expect(mockedAxios.post).toHaveBeenCalledWith('/api/feature-flags/beta/schedule', {
      flag: scheduled,
      scheduledAtUtc: '2026-05-27T14:00:00Z'
    });
    expect(result).toEqual(scheduled);
  });
});