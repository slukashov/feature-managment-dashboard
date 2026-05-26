import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import App from './App.tsx';
import { FeatureFlag } from './types/featureFlags';

const mockSaveFlag = vi.fn();
const mockToggleFlag = vi.fn();
const mockGetAuditHistory = vi.fn();
const mockRollbackFlag = vi.fn();
const mockDeleteFlag = vi.fn();
const mockGetActivityFeed = vi.fn();
let capturedNotify: ((message: string, severity: 'success' | 'error' | 'info' | 'warning') => void) | undefined;

const sampleFlag: FeatureFlag = {
  name: 'beta-flag',
  owner: 'team-beta',
  tags: ['beta'],
  requirementType: 0,
  enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }]
};

vi.mock('./hooks/useFeatureFlags', () => ({
  useFeatureFlags: (showNotification: (message: string, severity: 'success' | 'error' | 'info' | 'warning') => void) => {
    capturedNotify = showNotification;
    return {
      flags: [sampleFlag],
      isLoading: false,
      saveFlag: mockSaveFlag,
       toggleFlag: mockToggleFlag,
       getAuditHistory: mockGetAuditHistory,
       rollbackFlag: mockRollbackFlag,
       deleteFlag: mockDeleteFlag,
       getActivityFeed: mockGetActivityFeed
    };
  }
}));

vi.mock('./components/FeatureGrid', () => ({
  default: ({ onEdit, onToggle, onHistory, onDelete, onActivity }: { onEdit: (flag: FeatureFlag) => void; onToggle: (flag: FeatureFlag) => void; onHistory: (flag: FeatureFlag) => void; onDelete: (flag: FeatureFlag) => void; onActivity: (flag: FeatureFlag) => void }) => (
    <div>
      <button onClick={() => onEdit(sampleFlag)}>edit-flag</button>
      <button onClick={() => onToggle(sampleFlag)}>toggle-flag</button>
      <button onClick={() => onHistory(sampleFlag)}>history-flag</button>
      <button onClick={() => onActivity(sampleFlag)}>activity-flag</button>
      <button onClick={() => onDelete(sampleFlag)}>delete-flag</button>
    </div>
  )
}));

vi.mock('./components/FeatureDialog', () => ({
  default: ({
    open,
    onClose,
    onSave,
    initialData,
    isNew
  }: {
    open: boolean;
    onClose: () => void;
    onSave: (flag: FeatureFlag, isNew: boolean) => Promise<void>;
    initialData: FeatureFlag;
    isNew: boolean;
  }) =>
    open ? (
      <div>
        <div>dialog-open-{isNew ? 'new' : 'edit'}</div>
        <div>dialog-flag-{initialData.name || 'empty'}</div>
        <button onClick={() => onSave(initialData, isNew)}>dialog-save</button>
        <button onClick={onClose}>dialog-close</button>
      </div>
    ) : null
}));

vi.mock('./components/HistoryDialog', () => ({
  default: ({
    open,
    featureName,
    auditEntries,
    isLoading,
    onClose,
    onRollback
  }: {
    open: boolean;
    featureName: string;
    auditEntries: Array<{ id: number; snapshotVersion: number }>;
    isLoading: boolean;
    onClose: () => void;
    onRollback: (targetVersion: number) => Promise<void>;
  }) =>
    open ? (
      <div>
        <div>history-open-{featureName || 'empty'}</div>
        <div>history-loading-{isLoading ? 'yes' : 'no'}</div>
        <div>history-count-{auditEntries.length}</div>
        <button onClick={() => onRollback(1)}>history-rollback</button>
        <button onClick={onClose}>history-close</button>
      </div>
    ) : null
}));

vi.mock('./components/ActivityFeedDialog', () => ({
  default: ({
    open,
    featureName,
    activityEntries,
    isLoading,
    onClose
  }: {
    open: boolean;
    featureName: string;
    activityEntries: Array<{ id: number; activityType: string }>;
    isLoading: boolean;
    onClose: () => void;
  }) =>
    open ? (
      <div>
        <div>activity-open-{featureName || 'empty'}</div>
        <div>activity-loading-{isLoading ? 'yes' : 'no'}</div>
        <div>activity-count-{activityEntries.length}</div>
        <button onClick={onClose}>activity-close</button>
      </div>
    ) : null
}));

describe('App', () => {
  beforeEach(() => {
     vi.clearAllMocks();
     mockSaveFlag.mockResolvedValue(true);
     mockGetAuditHistory.mockResolvedValue([]);
     mockRollbackFlag.mockResolvedValue(true);
     mockDeleteFlag.mockResolvedValue(true);
     mockGetActivityFeed.mockResolvedValue([]);
     vi.spyOn(window, 'confirm').mockReturnValue(true);
     capturedNotify = undefined;
   });

  it('opens new dialog from action button', () => {
    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: 'New Feature Flag' }));

    expect(screen.getByText('dialog-open-new')).toBeInTheDocument();
    expect(screen.getByText('dialog-flag-empty')).toBeInTheDocument();
  });

  it('opens edit dialog from grid callback', () => {
    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: 'edit-flag' }));

    expect(screen.getByText('dialog-open-edit')).toBeInTheDocument();
    expect(screen.getByText('dialog-flag-beta-flag')).toBeInTheDocument();
  });

  it('closes dialog after successful save', async () => {
    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: 'New Feature Flag' }));
    fireEvent.click(screen.getByRole('button', { name: 'dialog-save' }));

    await waitFor(() => {
      expect(mockSaveFlag).toHaveBeenCalledTimes(1);
      expect(screen.queryByText('dialog-open-new')).not.toBeInTheDocument();
    });
  });

  it('keeps dialog open when save fails', async () => {
    mockSaveFlag.mockResolvedValue(false);

    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: 'New Feature Flag' }));
    fireEvent.click(screen.getByRole('button', { name: 'dialog-save' }));

    await waitFor(() => {
      expect(mockSaveFlag).toHaveBeenCalledTimes(1);
      expect(screen.getByText('dialog-open-new')).toBeInTheDocument();
    });
  });

  it('forwards grid toggle action to hook handler', () => {
    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: 'toggle-flag' }));

    expect(mockToggleFlag).toHaveBeenCalledWith(sampleFlag);
  });

  it('closes dialog through onClose callback', () => {
    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: 'New Feature Flag' }));
    expect(screen.getByText('dialog-open-new')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'dialog-close' }));
    expect(screen.queryByText('dialog-open-new')).not.toBeInTheDocument();
  });

  it('shows toast from notification callback', async () => {
    render(<App />);


    capturedNotify?.('From hook callback', 'error');

    await waitFor(() => {
      expect(screen.getByText('From hook callback')).toBeInTheDocument();
    });

    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => {
      expect(screen.queryByText('From hook callback')).not.toBeInTheDocument();
    });

    capturedNotify?.('Close via alert button', 'error');
    await waitFor(() => {
      expect(screen.getByText('Close via alert button')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /close/i }));
    await waitFor(() => {
      expect(screen.queryByText('Close via alert button')).not.toBeInTheDocument();
    });
  });

  it('opens history dialog and loads audit entries', async () => {
    mockGetAuditHistory.mockResolvedValue([
      {
        id: 1,
        featureFlagName: 'beta-flag',
        action: 1,
        snapshotVersion: 2,
        snapshotJson: '{}',
        changedAtUtc: '2026-05-26T10:00:00Z',
        changedBy: 'system'
      }
    ]);

    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: 'history-flag' }));

    await waitFor(() => {
      expect(mockGetAuditHistory).toHaveBeenCalledWith('beta-flag');
      expect(screen.getByText('history-open-beta-flag')).toBeInTheDocument();
      expect(screen.getByText('history-count-1')).toBeInTheDocument();
    });
  });

  it('runs rollback and closes history dialog on success', async () => {
    mockGetAuditHistory.mockResolvedValue([
      {
        id: 1,
        featureFlagName: 'beta-flag',
        action: 1,
        snapshotVersion: 1,
        snapshotJson: '{}',
        changedAtUtc: '2026-05-26T10:00:00Z',
        changedBy: 'system'
      }
    ]);

    render(<App />);
    fireEvent.click(screen.getByRole('button', { name: 'history-flag' }));

    await waitFor(() => {
      expect(screen.getByText('history-open-beta-flag')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'history-rollback' }));

    await waitFor(() => {
      expect(mockRollbackFlag).toHaveBeenCalledWith('beta-flag', 1);
      expect(screen.queryByText('history-open-beta-flag')).not.toBeInTheDocument();
    });
  });

   it('confirms delete and calls hook delete action', async () => {
     render(<App />);

     fireEvent.click(screen.getByRole('button', { name: 'delete-flag' }));

     await waitFor(() => {
       expect(window.confirm).toHaveBeenCalledWith('Delete feature flag "beta-flag"? This action cannot be undone.');
       expect(mockDeleteFlag).toHaveBeenCalledWith('beta-flag');
     });
   });

   it('opens activity feed and loads entries', async () => {
     mockGetActivityFeed.mockResolvedValue([
       {
         id: 1,
         featureFlagName: 'beta-flag',
         activityType: 'Updated',
         description: 'Enabled the feature',
         changeType: 'EnabledFor',
         changedAtUtc: '2026-05-26T10:00:00Z',
         changedBy: 'user@example.com'
       }
     ]);

     render(<App />);

     fireEvent.click(screen.getByRole('button', { name: 'activity-flag' }));

     await waitFor(() => {
       expect(mockGetActivityFeed).toHaveBeenCalledWith('beta-flag');
       expect(screen.getByText('activity-open-beta-flag')).toBeInTheDocument();
       expect(screen.getByText('activity-count-1')).toBeInTheDocument();
     });
   });
});