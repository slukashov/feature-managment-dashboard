import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import FeatureDialog from './FeatureDialog';
import { FeatureFlag } from '../types/featureFlags';

const emptyFlag: FeatureFlag = { name: '', requirementType: 0, enabledFor: [] };

describe('FeatureDialog', () => {
  it('disables save when name is empty and enables it after input', async () => {
    const onSave = vi.fn();

    render(
      <FeatureDialog
        open
        onClose={vi.fn()}
        onSave={onSave}
        initialData={emptyFlag}
        isNew
      />
    );

    const dialog = screen.getByRole('dialog', { name: 'Create New Feature' });
    const saveButton = within(dialog).getByRole('button', { name: 'Save Changes' });
    expect(saveButton).toBeDisabled();

    fireEvent.change(within(dialog).getByLabelText('Feature Name (Key)'), {
      target: { value: 'gamma-feature' }
    });

    expect(saveButton).toBeEnabled();

    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledWith(
        { name: 'gamma-feature', requirementType: 0, enabledFor: [] },
        true
      );
    });
  });

  it('toggles advanced rules block with the master switch', async () => {
    render(
      <FeatureDialog
        open
        onClose={vi.fn()}
        onSave={vi.fn()}
        initialData={emptyFlag}
        isNew
      />
    );

    expect(screen.queryByText('Advanced Targeting Rules')).not.toBeInTheDocument();

    const dialog = screen.getByRole('dialog', { name: 'Create New Feature' });
    fireEvent.click(within(dialog).getByRole('switch'));

    expect(await screen.findByText('Advanced Targeting Rules')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add Rule' })).toBeInTheDocument();
  });

  it('uses edit mode title and keeps feature name disabled', () => {
    const initialData: FeatureFlag = {
      name: 'existing-flag',
      requirementType: 1,
      enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }]
    };

    render(
      <FeatureDialog
        open
        onClose={vi.fn()}
        onSave={vi.fn()}
        initialData={initialData}
        isNew={false}
      />
    );

    const dialog = screen.getByRole('dialog', { name: 'Edit Feature Configuration' });
    expect(within(dialog).getByLabelText('Feature Name (Key)')).toBeDisabled();
  });

  it('adds, updates, removes rules and saves resulting configuration', async () => {
    const onSave = vi.fn();
    const initialData: FeatureFlag = {
      name: 'targeted-flag',
      requirementType: 0,
      enabledFor: [{ name: 'Microsoft.Percentage', parametersJson: '{"Value":50}' }]
    };

    render(
      <FeatureDialog
        open
        onClose={vi.fn()}
        onSave={onSave}
        initialData={initialData}
        isNew={false}
      />
    );

    const dialog = screen.getByRole('dialog', { name: 'Edit Feature Configuration' });

    fireEvent.click(within(dialog).getByRole('button', { name: 'Add Rule' }));
    expect(within(dialog).getAllByLabelText('Percentage (0-100)')).toHaveLength(2);

    fireEvent.change(within(dialog).getAllByLabelText('Percentage (0-100)')[0], {
      target: { value: '80' }
    });

    fireEvent.click(within(dialog).getAllByRole('button', { name: 'Remove Rule' })[1]);
    fireEvent.click(within(dialog).getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledWith(
        {
          name: 'targeted-flag',
          requirementType: 0,
          enabledFor: [{ name: 'Microsoft.Percentage', parametersJson: '{"Value":80}' }]
        },
        false
      );
    });
  });

  it('turns master switch off and saves without rules', async () => {
    const onSave = vi.fn();
    const initialData: FeatureFlag = {
      name: 'switch-off-flag',
      requirementType: 1,
      enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }]
    };

    render(
      <FeatureDialog
        open
        onClose={vi.fn()}
        onSave={onSave}
        initialData={initialData}
        isNew={false}
      />
    );

    const dialog = screen.getByRole('dialog', { name: 'Edit Feature Configuration' });
    fireEvent.click(within(dialog).getByRole('switch'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledWith(
        {
          name: 'switch-off-flag',
          requirementType: 1,
          enabledFor: []
        },
        false
      );
    });
  });

  it('changes rule requirement to ALL and saves updated requirement type', async () => {
    const onSave = vi.fn();
    const initialData: FeatureFlag = {
      name: 'requirement-flag',
      requirementType: 0,
      enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }]
    };

    render(
      <FeatureDialog
        open
        onClose={vi.fn()}
        onSave={onSave}
        initialData={initialData}
        isNew={false}
      />
    );

    const dialog = screen.getByRole('dialog', { name: 'Edit Feature Configuration' });
    fireEvent.mouseDown(within(dialog).getByRole('combobox', { name: 'Rule Requirement' }));
    fireEvent.click(screen.getByRole('option', { name: 'Match ALL rules (AND)' }));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledWith(
        {
          name: 'requirement-flag',
          requirementType: 1,
          enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }]
        },
        false
      );
    });
  });

  it('calls onClose when cancel is clicked', () => {
    const onClose = vi.fn();

    render(
      <FeatureDialog
        open
        onClose={onClose}
        onSave={vi.fn()}
        initialData={emptyFlag}
        isNew
      />
    );

    const dialog = screen.getByRole('dialog', { name: 'Create New Feature' });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });
});