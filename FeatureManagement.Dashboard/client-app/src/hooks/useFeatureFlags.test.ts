import { renderHook, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useFeatureFlags } from './useFeatureFlags';
import { featureFlagsApi } from '../api/featureFlagsApi';
import { FeatureFlag } from '../types/featureFlags';

vi.mock('../api/featureFlagsApi', () => ({
  featureFlagsApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn()
  }
}));

const api = vi.mocked(featureFlagsApi);

const sampleFlags: FeatureFlag[] = [
  { name: 'alpha', requirementType: 0, enabledFor: [] },
  { name: 'beta', requirementType: 1, enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }] }
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
    api.create.mockResolvedValue({ name: 'new-flag', requirementType: 0, enabledFor: [] });
    const notify = vi.fn();

    const { result } = renderHook(() => useFeatureFlags(notify));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    const newFlag: FeatureFlag = { name: 'new-flag', requirementType: 0, enabledFor: [] };

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
      await result.current.toggleFlag({ name: 'alpha', requirementType: 0, enabledFor: [] });
    });

    expect(api.update).toHaveBeenCalledWith('alpha', {
      name: 'alpha',
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
});