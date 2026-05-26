import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ActivityFeedDialog from './ActivityFeedDialog';
import { FeatureFlagActivityEntry } from '../types/featureFlags';

describe('ActivityFeedDialog', () => {
  it('displays loading spinner when loading', () => {
    render(
      <ActivityFeedDialog
        open
        featureName="test-flag"
        activityEntries={[]}
        isLoading
        onClose={vi.fn()}
      />
    );

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('displays empty message when no entries', () => {
    render(
      <ActivityFeedDialog
        open
        featureName="test-flag"
        activityEntries={[]}
        isLoading={false}
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText('No activity recorded for this feature flag.')).toBeInTheDocument();
  });

  it('displays activity entries in order', () => {
    const entries: FeatureFlagActivityEntry[] = [
      {
        id: 1,
        featureFlagName: 'test-flag',
        activityType: 'Created',
        description: 'Feature flag created',
        changeType: undefined,
        changedAtUtc: '2026-05-26T10:00:00Z',
        changedBy: 'user@example.com'
      },
      {
        id: 2,
        featureFlagName: 'test-flag',
        activityType: 'Updated',
        description: 'Enabled for beta users',
        changeType: 'EnabledFor',
        changedAtUtc: '2026-05-26T11:00:00Z',
        changedBy: 'admin@example.com'
      }
    ];

    render(
      <ActivityFeedDialog
        open
        featureName="test-flag"
        activityEntries={entries}
        isLoading={false}
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText('Feature flag created')).toBeInTheDocument();
    expect(screen.getByText('Enabled for beta users')).toBeInTheDocument();
    expect(screen.getByText('Created')).toBeInTheDocument();
    expect(screen.getByText('Updated')).toBeInTheDocument();
  });

  it('shows activity type labels', () => {
    const entries: FeatureFlagActivityEntry[] = [
      {
        id: 1,
        featureFlagName: 'test-flag',
        activityType: 'Scheduled',
        description: 'Scheduled rollout',
        changeType: 'ScheduledAtUtc',
        changedAtUtc: '2026-05-26T10:00:00Z',
        changedBy: 'system'
      }
    ];

    render(
      <ActivityFeedDialog
        open
        featureName="test-flag"
        activityEntries={entries}
        isLoading={false}
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText('Scheduled')).toBeInTheDocument();
    expect(screen.getByText(/ScheduledAtUtc/)).toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', () => {
    const onClose = vi.fn();

    render(
      <ActivityFeedDialog
        open
        featureName="test-flag"
        activityEntries={[]}
        isLoading={false}
        onClose={onClose}
      />
    );

    const closeButton = screen.getByRole('button', { name: 'Close' });
    closeButton.click();

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('displays title with feature name', () => {
    render(
      <ActivityFeedDialog
        open
        featureName="my-awesome-feature"
        activityEntries={[]}
        isLoading={false}
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText('Activity Feed: my-awesome-feature')).toBeInTheDocument();
  });
});





