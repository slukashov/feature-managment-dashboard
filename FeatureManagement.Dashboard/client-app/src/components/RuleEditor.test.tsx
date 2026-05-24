import { fireEvent, render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import RuleEditor from './RuleEditor';

function renderWithDateContext(ui: React.ReactNode) {
  return render(
    <LocalizationProvider dateAdapter={AdapterDayjs}>
      {ui}
    </LocalizationProvider>
  );
}

describe('RuleEditor', () => {
  it('updates percentage value', () => {
    const onUpdate = vi.fn();

    renderWithDateContext(
      <RuleEditor
        index={0}
        filter={{ name: 'Microsoft.Percentage', parametersJson: '{"Value":50}' }}
        onUpdate={onUpdate}
        onRemove={vi.fn()}
      />
    );

    fireEvent.change(screen.getByLabelText('Percentage (0-100)'), {
      target: { value: '80' }
    });

    expect(onUpdate).toHaveBeenCalledWith(0, {
      name: 'Microsoft.Percentage',
      parametersJson: '{"Value":80}'
    });

    fireEvent.change(screen.getByLabelText('Percentage (0-100)'), {
      target: { value: 'abc' }
    });

    expect(onUpdate).toHaveBeenCalledWith(0, {
      name: 'Microsoft.Percentage',
      parametersJson: '{"Value":0}'
    });
  });

  it('shows time window controls for Microsoft.TimeWindow', () => {
    renderWithDateContext(
      <RuleEditor
        index={0}
        filter={{ name: 'Microsoft.TimeWindow', parametersJson: '{"Start":"","End":""}' }}
        onUpdate={vi.fn()}
        onRemove={vi.fn()}
      />
    );

    expect(screen.getAllByText('Start Date').length).toBeGreaterThan(0);
    expect(screen.getAllByText('End Date').length).toBeGreaterThan(0);
  });

  it('passes through invalid custom JSON', () => {
    const onUpdate = vi.fn();

    renderWithDateContext(
      <RuleEditor
        index={1}
        filter={{ name: 'Custom', parametersJson: '{}' }}
        onUpdate={onUpdate}
        onRemove={vi.fn()}
      />
    );

    fireEvent.change(screen.getByLabelText('Parameters (JSON)'), {
      target: { value: '{bad-json' }
    });

    expect(onUpdate).toHaveBeenCalledWith(1, {
      name: 'Custom',
      parametersJson: '{bad-json'
    });

    fireEvent.change(screen.getByLabelText('Parameters (JSON)'), {
      target: { value: '' }
    });

    expect(onUpdate).toHaveBeenCalledWith(1, {
      name: 'Custom',
      parametersJson: '{}'
    });
  });

  it('falls back to default percentage when parameters JSON is invalid', () => {
    renderWithDateContext(
      <RuleEditor
        index={2}
        filter={{ name: 'Microsoft.Percentage', parametersJson: '{invalid-json' }}
        onUpdate={vi.fn()}
        onRemove={vi.fn()}
      />
    );

    expect(screen.getByLabelText('Percentage (0-100)')).toHaveValue(50);
  });

  it('uses empty JSON fallback when parametersJson is empty', () => {
    renderWithDateContext(
      <RuleEditor
        index={5}
        filter={{ name: 'Microsoft.Percentage', parametersJson: '' }}
        onUpdate={vi.fn()}
        onRemove={vi.fn()}
      />
    );

    expect(screen.getByLabelText('Percentage (0-100)')).toHaveValue(50);
  });

  it('initializes params when rule type is changed from AlwaysOn', () => {
    const onUpdate = vi.fn();

    renderWithDateContext(
      <RuleEditor
        index={0}
        filter={{ name: 'AlwaysOn', parametersJson: '{}' }}
        onUpdate={onUpdate}
        onRemove={vi.fn()}
      />
    );

    fireEvent.mouseDown(screen.getByRole('combobox', { name: 'Rule Type' }));
    fireEvent.click(screen.getByRole('option', { name: 'Percentage Rollout' }));

    fireEvent.mouseDown(screen.getByRole('combobox', { name: 'Rule Type' }));
    fireEvent.click(screen.getByRole('option', { name: 'Time Window' }));

    expect(onUpdate).toHaveBeenNthCalledWith(1, 0, {
      name: 'Microsoft.Percentage',
      parametersJson: '{"Value":50}'
    });
    expect(onUpdate).toHaveBeenNthCalledWith(2, 0, {
      name: 'Microsoft.TimeWindow',
      parametersJson: '{"Start":"","End":""}'
    });
  });

  it('calls remove handler', () => {
    const onRemove = vi.fn();

    renderWithDateContext(
      <RuleEditor
        index={3}
        filter={{ name: 'AlwaysOn', parametersJson: '{}' }}
        onUpdate={vi.fn()}
        onRemove={onRemove}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: 'Remove Rule' }));

    expect(onRemove).toHaveBeenCalledWith(3);
  });
});