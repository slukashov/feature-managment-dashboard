import { fireEvent, render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import dayjs from 'dayjs';

vi.mock('@mui/x-date-pickers/DateTimePicker', () => ({
  DateTimePicker: ({ label, value, onChange }: { label: string; value: unknown; onChange: (value: any) => void }) => (
    <div>
      <div>{label}-value-{value ? 'set' : 'null'}</div>
      <button onClick={() => onChange(dayjs('2026-05-23T10:30'))}>{label}-set</button>
      <button onClick={() => onChange(null)}>{label}-clear</button>
    </div>
  )
}));

import RuleEditor from './RuleEditor';

describe('RuleEditor date conversion', () => {
  it('maps invalid time-window values to null picker values and formats changed dates', () => {
    const onUpdate = vi.fn();

    render(
      <RuleEditor
        index={0}
        filter={{ name: 'Microsoft.TimeWindow', parametersJson: '{"Start":"not-a-date","End":""}' }}
        onUpdate={onUpdate}
        onRemove={vi.fn()}
      />
    );

    expect(screen.getByText('Start Date-value-null')).toBeInTheDocument();
    expect(screen.getByText('End Date-value-null')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Start Date-set' }));
    expect(onUpdate).toHaveBeenCalledWith(0, {
      name: 'Microsoft.TimeWindow',
      parametersJson: '{"Start":"2026-05-23T10:30","End":""}'
    });

    fireEvent.click(screen.getByRole('button', { name: 'End Date-clear' }));
    expect(onUpdate).toHaveBeenCalledWith(0, {
      name: 'Microsoft.TimeWindow',
      parametersJson: '{"Start":"not-a-date","End":""}'
    });
  });

  it('maps valid stored time-window values to non-null picker values', () => {
    render(
      <RuleEditor
        index={0}
        filter={{ name: 'Microsoft.TimeWindow', parametersJson: '{"Start":"2026-05-23T10:30","End":"2026-05-23T11:30"}' }}
        onUpdate={vi.fn()}
        onRemove={vi.fn()}
      />
    );

    expect(screen.getByText('Start Date-value-set')).toBeInTheDocument();
    expect(screen.getByText('End Date-value-set')).toBeInTheDocument();
  });
});