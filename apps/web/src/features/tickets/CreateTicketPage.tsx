import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ErrorMessage } from '../../components/ErrorMessage';
import { Spinner } from '../../components/Spinner';
import { useReferenceData } from '../../hooks/useReferenceData';
import type { CreateTicketResponse } from '../../types/api';
import { CreateTicketForm } from './components/CreateTicketForm';
import { PriorityBadge } from './components/TicketBadges';
import { SlaIndicator } from './components/SlaCell';

export function CreateTicketPage() {
  const reference = useReferenceData();
  const navigate = useNavigate();
  const [created, setCreated] = useState<CreateTicketResponse>();

  return (
    <section>
      <header className="page-header">
        <div>
          <p className="breadcrumb">
            <Link to="/tickets">Tickets</Link> / New
          </p>
          <h1>Raise a ticket</h1>
        </div>
      </header>

      {reference.isLoading && (
        <Spinner label="Loading customers and categories" />
      )}

      {reference.error && (
        <ErrorMessage
          message={reference.error}
          onRetry={() => void reference.reload()}
        />
      )}

      {created && (
        <div className="card notice" role="status">
          <h2>Ticket raised</h2>
          <p>
            <strong>{created.ticket.reference}</strong> for{' '}
            {created.ticket.customer.name}.
          </p>

          <dl className="triage-summary">
            <div>
              <dt>Priority</dt>
              <dd>
                <PriorityBadge priority={created.ticket.priority} />
                <span className="summary__note">
                  {created.triage.priorityReason}
                </span>
              </dd>
            </div>
            <div>
              <dt>SLA</dt>
              <dd>
                <SlaIndicator
                  slaStatus={created.ticket.slaStatus}
                  dueAtUtc={created.ticket.dueAtUtc}
                />
                <span className="summary__note">
                  {created.triage.slaReason}
                </span>
              </dd>
            </div>
            <div>
              <dt>Assignment</dt>
              <dd>
                {created.ticket.assignedAgent?.fullName ?? 'Unassigned'}
                <span className="summary__note">
                  {created.triage.assignmentReason}
                </span>
              </dd>
            </div>
          </dl>

          <div className="button-row">
            <button
              type="button"
              className="button button--primary"
              onClick={() => void navigate(`/tickets/${created.ticket.id}`)}
            >
              Open {created.ticket.reference}
            </button>
            <button
              type="button"
              className="button button--ghost"
              onClick={() => setCreated(undefined)}
            >
              Raise another
            </button>
          </div>
        </div>
      )}

      {reference.data && !created && (
        <CreateTicketForm
          customers={reference.data.customers}
          categories={reference.data.categories}
          onCreated={setCreated}
        />
      )}
    </section>
  );
}