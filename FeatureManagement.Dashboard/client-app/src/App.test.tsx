import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import App from './App.tsx';
import { FeatureFlag } from './types/featureFlags';

const mockSaveFlag = vi.fn();
const mockToggleFlag = vi.fn();
let capturedNotify: ((message: string, severity: 'success' | 'error' | 'info' | 'warning') => void) | undefined;

const sampleFlag: FeatureFlag = {
  name: 'beta-flag',
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
      toggleFlag: mockToggleFlag
    };
  }
}));

vi.mock('./components/FeatureGrid', () => ({
  default: ({ onEdit, onToggle }: { onEdit: (flag: FeatureFlag) => void; onToggle: (flag: FeatureFlag) => void }) => (
    <div>
      <button onClick={() => onEdit(sampleFlag)}>edit-flag</button>
      <button onClick={() => onToggle(sampleFlag)}>toggle-flag</button>
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

describe('App', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockSaveFlag.mockResolvedValue(true);
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

  it('toggles theme and shows toast from notification callback', async () => {
    render(<App />);

    expect(screen.getByTestId('DarkModeIcon')).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText('toggle-theme'));
    expect(screen.getByTestId('LightModeIcon')).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText('toggle-theme'));
    expect(screen.getByTestId('DarkModeIcon')).toBeInTheDocument();

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
});







