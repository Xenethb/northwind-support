import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ticketsApi } from '../../../api/tickets';
import { ApiError } from '../../../api/client';
import type { TicketDetail } from '../../../types/api';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    escalate: vi.fn(),
    getEscalations: vi.fn(),
  },
}));

const escalate = vi.mocked(ticketsApi.escalate);
const getEscalations = vi.mocked(ticketsApi.getEscalations);

function makeTicket(
  overrides: Partial<TicketDetail> = {},
): TicketDetail {
  return {
    id: 12,
    reference: 'TCK-0012',
    title: 'Invoice total does not match the order',
    description: 'The numbers on the PDF do not add up.',
    status: 'InProgress',
    priority: 'High',
    customer: {
      id: 2,
      name: 'Fabrikam Inc',
      tier: 'Standard',
      email: 'billing@fabrikam.com',
      phone: null,
    },
    category: {
      id: 2,
      name: 'Billing',
      requiresSpecialist: true,
    },
    assignedAgent: {
      id: 4,
      fullName: 'Sara Lindqvist',
    },
    createdAtUtc: '2026-09-24T08:12:00Z',
    updatedAtUtc: '2026-09-25T10:03:00Z',
    dueAtUtc: '2026-09-24T16:12:00Z',
    resolvedAtUtc: null,
    slaStatus: 'Breached',
    ...overrides,
  };
}

describe('EscalationPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getEscalations.mockResolvedValue([]);
  });

  it('blocks submission when the reason is too short and does not call the API', async () => {
    const user = userEvent.setup();
    const onEscalated = vi.fn();

    render(
      <EscalationPanel
        ticket={makeTicket()}
        onEscalated={onEscalated}
      />,
    );

    const textarea = screen.getByLabelText(/reason/i);
    const button = screen.getByRole('button', {
      name: /escalate ticket/i,
    });

    await user.type(textarea, 'hi');

    expect(button).toBeDisabled();

    await user.click(button);

    expect(escalate).not.toHaveBeenCalled();
    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('shows a 409 error and does not change the ticket', async () => {
    const user = userEvent.setup();
    const onEscalated = vi.fn();
    const ticket = makeTicket();

    escalate.mockRejectedValue(
      new ApiError(409, 'Critical tickets cannot be escalated.'),
    );

    render(
      <EscalationPanel
        ticket={ticket}
        onEscalated={onEscalated}
      />,
    );

    const textarea = screen.getByLabelText(/reason/i);

    await user.type(
      textarea,
      'Customer is losing money every hour – needs higher priority now.',
    );

    const button = screen.getByRole('button', {
      name: /escalate ticket/i,
    });

    await user.click(button);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        /critical tickets cannot be escalated/i,
      );
    });

    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('disables the form when the ticket cannot be escalated', async () => {
    await act(async () => {
      render(
        <EscalationPanel
          ticket={makeTicket({ priority: 'Critical' })}
          onEscalated={vi.fn()}
        />,
      );
    });

    // Wait for the history load to finish so no more state updates happen.
    await waitFor(() => {
      expect(
        screen.getByText(/critical tickets cannot be escalated/i),
      ).toBeInTheDocument();
    });

    expect(
      screen.queryByRole('button', { name: /escalate ticket/i }),
    ).not.toBeInTheDocument();
  });
});