import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { SlaStatus } from '../../../types/api';
import { SlaIndicator } from './SlaCell';

const allStatuses: SlaStatus[] = [
  'WithinSla',
  'AtRisk',
  'Breached',
  'Met',
  'NotApplicable',
];

const expectedLabels: Record<SlaStatus, string> = {
  WithinSla: 'Within SLA',
  AtRisk: 'At risk',
  Breached: 'Breached',
  Met: 'Met',
  NotApplicable: 'No SLA',
};

describe('SlaIndicator', () => {
  it.each(allStatuses)('renders the %s state distinguishably', (status) => {
    render(<SlaIndicator slaStatus={status} />);

    expect(screen.getByText(expectedLabels[status])).toBeInTheDocument();
    // The container carries the status so colour is not the only signal
    expect(document.querySelector(`[data-status="${status}"]`)).toBeInTheDocument();
  });

  it('shows the due date when not compact', () => {
    render(
      <SlaIndicator
        slaStatus="WithinSla"
        dueAtUtc="2026-10-15T12:00:00Z"
        compact={false}
      />,
    );

    // formatDateTime will produce a readable string – we just check something is rendered
    expect(screen.getByText(/2026|15|12/)).toBeInTheDocument();
  });
});