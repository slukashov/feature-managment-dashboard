import { renderHook, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useFeatureFlags } from './useFeatureFlags';
import { featureFlagsApi } from '../api/featureFlagsApi';
import { FeatureFlag, FeatureFlagAuditLog } from '../types/featureFlags';

vi.mock('../api/featureFlagsApi', () => ({
  featureFlagsApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    getAudit: vi.fn(),
    rollback: vi.fn(),
    delete: vi.fn(),
    getActivity: vi.fn(),
    scheduleChange: vi.fn()
  }
}));

const api = vi.mocked(featureFlagsApi);

const sampleFlags: FeatureFlag[] = [
  { name: 'alpha', owner: 'team-a', tags: ['alpha'], requirementType: 0, enabledFor: [] },
  { name: 'beta', owner: 'team-b', tags: ['beta'], requirementType: 1, enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }] }
];

describe('useFeatureFlags', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('loads flags on mount and clears loading state', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(api.getAll).toHaveBeenCalledTimes(1);
    expect(result.current.flags).toEqual(sampleFlags);
    expect(notify).not.toHaveBeenCalled();
  });

  it('creates a new flag and refreshes data', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.create.mockResolvedValue({ name: 'new-flag', owner: 'team-a', tags: ['new'], requirementType: 0, enabledFor: [] });
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    const newFlag: FeatureFlag = { name: 'new-flag', owner: 'team-a', tags: ['new'], requirementType: 0, enabledFor: [] };

    let success = false;
    await act(async () => {
      success = await result.current.saveFlag(newFlag, true);
    });

    expect(success).toBe(true);
    expect(api.create).toHaveBeenCalledWith(newFlag);
    expect(api.getAll).toHaveBeenCalledTimes(2);
    expect(notify).toHaveBeenCalledWith('Feature created successfully!', 'success');
  });

  it('updates an existing flag and refreshes data', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.update.mockResolvedValue();
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let success = false;
    await act(async () => {
      success = await result.current.saveFlag(sampleFlags[0], false);
    });

    expect(success).toBe(true);
    expect(api.update).toHaveBeenCalledWith('alpha', sampleFlags[0]);
    expect(notify).toHaveBeenCalledWith('Feature updated successfully!', 'success');
  });

  it('toggles a disabled flag on and refreshes', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.update.mockResolvedValue();
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(async () => {
      await result.current.toggleFlag({ name: 'alpha', owner: 'team-a', tags: ['alpha'], requirementType: 0, enabledFor: [] });
    });

    expect(api.update).toHaveBeenCalledWith('alpha', {
      name: 'alpha',
      owner: 'team-a',
      tags: ['alpha'],
      requirementType: 0,
      enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }]
    });
    expect(api.getAll).toHaveBeenCalledTimes(2);
    expect(notify).toHaveBeenCalledWith('alpha turned ON', 'success');
  });

  it('toggles an enabled flag off and refreshes', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.update.mockResolvedValue();
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(async () => {
      await result.current.toggleFlag(sampleFlags[1]);
    });

    expect(api.update).toHaveBeenCalledWith('beta', {
      name: 'beta',
      owner: 'team-b',
      tags: ['beta'],
      requirementType: 1,
      enabledFor: []
    });
    expect(notify).toHaveBeenCalledWith('beta turned OFF', 'success');
  });

  it('returns false and notifies on save failure', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.update.mockRejectedValue(new Error('boom'));
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let success = true;
    await act(async () => {
      success = await result.current.saveFlag(sampleFlags[0], false);
    });

    expect(success).toBe(false);
    expect(notify).toHaveBeenCalledWith('Failed to save feature flag.', 'error');
  });

  it('notifies when initial fetch fails', async () => {
    api.getAll.mockRejectedValue(new Error('network'));
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.flags).toEqual([]);
    expect(notify).toHaveBeenCalledWith('Failed to load feature flags.', 'error');
  });

  it('notifies when toggle fails', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.update.mockRejectedValue(new Error('update failed'));
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(async () => {
      await result.current.toggleFlag(sampleFlags[1]);
    });

    expect(notify).toHaveBeenCalledWith('Failed to toggle feature flag.', 'error');
  });

  it('loads audit history for a feature', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.getAudit.mockResolvedValue([
      {
        id: 2,
        featureFlagName: 'beta',
        action: 1,
        snapshotVersion: 2,
        snapshotJson: '{}',
        changedAtUtc: '2026-05-26T10:00:00Z',
        changedBy: 'system'
      }
    ]);
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let history: FeatureFlagAuditLog[] | null = null;
    await act(async () => {
      history = await result.current.getAuditHistory('beta');
    });

    expect(api.getAudit).toHaveBeenCalledWith('beta');
    expect(history).toHaveLength(1);
    expect(notify).not.toHaveBeenCalledWith('Failed to load feature history.', 'error');
  });

  it('notifies when loading audit history fails', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.getAudit.mockRejectedValue(new Error('history failed'));
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let history = undefined;
    await act(async () => {
      history = await result.current.getAuditHistory('beta');
    });

    expect(history).toBeNull();
    expect(notify).toHaveBeenCalledWith('Failed to load feature history.', 'error');
  });

  it('rolls back a feature and refreshes data', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.rollback.mockResolvedValue({ ...sampleFlags[1], version: 3 });
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let success = false;
    await act(async () => {
      success = await result.current.rollbackFlag('beta', 1);
    });

    expect(success).toBe(true);
    expect(api.rollback).toHaveBeenCalledWith('beta', 1);
    expect(api.getAll).toHaveBeenCalledTimes(2);
    expect(notify).toHaveBeenCalledWith('beta rolled back to version 1.', 'success');
  });

  it('notifies when rollback fails', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.rollback.mockRejectedValue(new Error('rollback failed'));
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let success = true;
    await act(async () => {
      success = await result.current.rollbackFlag('beta', 1);
    });

    expect(success).toBe(false);
    expect(notify).toHaveBeenCalledWith('Failed to rollback feature flag.', 'error');
  });

  it('deletes a feature and refreshes data', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.delete.mockResolvedValue(undefined);
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let success = false;
    await act(async () => {
      success = await result.current.deleteFlag('beta');
    });

    expect(success).toBe(true);
    expect(api.delete).toHaveBeenCalledWith('beta');
    expect(api.getAll).toHaveBeenCalledTimes(2);
    expect(notify).toHaveBeenCalledWith('Feature beta deleted successfully!', 'success');
  });

  it('notifies when delete fails', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.delete.mockRejectedValue(new Error('delete failed'));
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let success = true;
    await act(async () => {
      success = await result.current.deleteFlag('beta');
    });

    expect(success).toBe(false);
    expect(notify).toHaveBeenCalledWith('Failed to delete feature flag.', 'error');
  });

  it('loads activity feed for a feature', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.getActivity.mockResolvedValue([
      {
        id: 1,
        featureFlagName: 'beta',
        activityType: 'Updated',
        description: 'Enabled the feature',
        changeType: 'EnabledFor',
        changedAtUtc: '2026-05-26T10:00:00Z',
        changedBy: 'system'
      }
    ]);
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let activity = null;
    await act(async () => {
      activity = await result.current.getActivityFeed('beta');
    });

    expect(api.getActivity).toHaveBeenCalledWith('beta');
    expect(activity).toHaveLength(1);
  });

  it('schedules a feature flag change', async () => {
    api.getAll.mockResolvedValue(sampleFlags);
    api.scheduleChange.mockResolvedValue({ ...sampleFlags[1], scheduledAtUtc: '2026-05-27T14:00:00Z' });
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    let success = false;
    await act(async () => {
      success = await result.current.scheduleChange('beta', sampleFlags[1], '2026-05-27T14:00:00Z');
    });

    expect(success).toBe(true);
    expect(api.scheduleChange).toHaveBeenCalledWith('beta', sampleFlags[1], '2026-05-27T14:00:00Z');
    expect(api.getAll).toHaveBeenCalledTimes(2);
    expect(notify).toHaveBeenCalled();
  });
});