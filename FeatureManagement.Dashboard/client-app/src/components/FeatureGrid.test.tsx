import { fireEvent, render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import FeatureGrid from './FeatureGrid';
import { FeatureFlag } from '../types/featureFlags';

const flags: FeatureFlag[] = [
  { name: 'alpha-feature', owner: 'team-alpha', tags: ['alpha', 'core'], requirementType: 0, enabledFor: [] },
  { name: 'beta-feature', owner: 'team-beta', tags: ['beta'], requirementType: 1, enabledFor: [{ name: 'AlwaysOn', parametersJson: '{}' }] }
];

describe('FeatureGrid', () => {
  it('renders loading skeleton cards when loading is true', () => {
    render(
       <FeatureGrid
         flags={flags}
         isLoading
         onToggle={vi.fn()}
         onEdit={vi.fn()}
         onHistory={vi.fn()}
         onDelete={vi.fn()}
         onActivity={vi.fn()}
       />
    );

    expect(screen.getAllByText((_, element) => element?.tagName.toLowerCase() === 'span').length).toBeGreaterThan(0);
  });

  it('filters by search text', () => {
    render(
       <FeatureGrid
         flags={flags}
         isLoading={false}
         onToggle={vi.fn()}
         onEdit={vi.fn()}
         onHistory={vi.fn()}
         onDelete={vi.fn()}
         onActivity={vi.fn()}
       />
     );

     expect(screen.getByText('alpha-feature')).toBeInTheDocument();
    expect(screen.getByText('beta-feature')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search by feature name'), {
      target: { value: 'alpha' }
    });

    expect(screen.getByText('alpha-feature')).toBeInTheDocument();
    expect(screen.queryByText('beta-feature')).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search by feature name'), {
      target: { value: 'team-beta' }
    });
    expect(screen.getByText('beta-feature')).toBeInTheDocument();
    expect(screen.queryByText('alpha-feature')).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search by feature name'), {
      target: { value: 'core' }
    });
    expect(screen.getByText('alpha-feature')).toBeInTheDocument();
  });

   it('filters by enabled status', () => {
     render(
       <FeatureGrid
         flags={flags}
         isLoading={false}
         onToggle={vi.fn()}
         onEdit={vi.fn()}
         onHistory={vi.fn()}
         onDelete={vi.fn()}
         onActivity={vi.fn()}
       />
     );

     fireEvent.click(screen.getAllByRole('button', { name: 'Enabled' })[0]);

    expect(screen.queryByText('alpha-feature')).not.toBeInTheDocument();
    expect(screen.getByText('beta-feature')).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole('button', { name: 'Disabled' })[0]);
    expect(screen.getByText('alpha-feature')).toBeInTheDocument();
    expect(screen.queryByText('beta-feature')).not.toBeInTheDocument();

    // Clicking active value returns null in exclusive mode; ensure it does not throw and keeps state.
    fireEvent.click(screen.getAllByRole('button', { name: 'Disabled' })[0]);
    expect(screen.getByText('alpha-feature')).toBeInTheDocument();
  });

   it('calls handlers for toggle, configure, history, and delete actions', () => {
     const onToggle = vi.fn();
     const onEdit = vi.fn();
     const onHistory = vi.fn();
     const onDelete = vi.fn();
     const onActivity = vi.fn();

     render(
       <FeatureGrid
         flags={flags}
         isLoading={false}
         onToggle={onToggle}
         onEdit={onEdit}
         onHistory={onHistory}
         onDelete={onDelete}
         onActivity={onActivity}
       />
     );

     const switches = screen.getAllByRole('switch');
     fireEvent.click(switches[0]);
     fireEvent.click(screen.getAllByRole('button', { name: 'Configure' })[0]);
     fireEvent.click(screen.getAllByRole('button', { name: 'History' })[0]);
     fireEvent.click(screen.getAllByRole('button', { name: 'Activity' })[0]);
     fireEvent.click(screen.getAllByRole('button', { name: 'Delete' })[0]);

     expect(onToggle).toHaveBeenCalledWith(flags[0]);
     expect(onEdit).toHaveBeenCalledWith(flags[0]);
     expect(onHistory).toHaveBeenCalledWith(flags[0]);
     expect(onActivity).toHaveBeenCalledWith(flags[0]);
     expect(onDelete).toHaveBeenCalledWith(flags[0]);
  });

  it('opens edit when clicking card or feature name', () => {
    const onEdit = vi.fn();

     const { container } = render(
       <FeatureGrid
         flags={flags}
         isLoading={false}
         onToggle={vi.fn()}
         onEdit={onEdit}
         onHistory={vi.fn()}
         onDelete={vi.fn()}
         onActivity={vi.fn()}
       />
     );

     fireEvent.click(screen.getByText('alpha-feature'));

    const cards = container.querySelectorAll('.MuiCard-root');
    fireEvent.click(cards[1]);

    expect(onEdit).toHaveBeenNthCalledWith(1, flags[0]);
    expect(onEdit).toHaveBeenNthCalledWith(2, flags[0]);
  });

  it('shows empty state when no flags match', () => {
     render(
       <FeatureGrid
         flags={flags}
         isLoading={false}
         onToggle={vi.fn()}
         onEdit={vi.fn()}
         onHistory={vi.fn()}
         onDelete={vi.fn()}
         onActivity={vi.fn()}
       />
     );

     fireEvent.change(screen.getByLabelText('Search by feature name'), {
       target: { value: 'not-found' }
     });

    expect(screen.getByText('No matching feature flags')).toBeInTheDocument();
    expect(screen.getByText('Try changing filters or create a new feature flag.')).toBeInTheDocument();
  });
});