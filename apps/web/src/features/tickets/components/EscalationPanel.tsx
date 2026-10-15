
import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { formatDateTime } from '../../../lib/format';
import type { TicketDetail, TicketEscalation } from '../../../types/api';
import { PriorityBadge } from './TicketBadges';

interface EscalationPanelProps {
  ticket: TicketDetail;
  onEscalated: (updated: TicketDetail) => void;
}

const MIN_REASON = 5;
const MAX_REASON = 500;

function canEscalate(ticket: TicketDetail): boolean {
  return (
    ticket.priority !== 'Critical' &&
    ticket.status !== 'Resolved' &&
    ticket.status !== 'Closed'
  );
}

export function EscalationPanel({
  ticket,
  onEscalated,
}: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [history, setHistory] = useState<TicketEscalation[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(true);

  // Fetch escalation history from the API.
  const loadHistory = useCallback(async (): Promise<TicketEscalation[]> => {
    return ticketsApi.getEscalations(ticket.id);
  }, [ticket.id]);

  // Load history when the panel mounts or the ticket changes.
  useEffect(() => {
    let cancelled = false;

    const fetchHistory = async () => {
      try {
        const data = await loadHistory();

        if (!cancelled) {
          setHistory(data);
          setIsLoadingHistory(false);
        }
      } catch {
        // History is secondary and must not block the panel.
        if (!cancelled) {
          setHistory([]);
          setIsLoadingHistory(false);
        }
      }
    };

    void fetchHistory();

    return () => {
      cancelled = true;
    };
  }, [loadHistory]);

  const reasonLength = reason.trim().length;

  const isReasonValid =
    reasonLength >= MIN_REASON &&
    reasonLength <= MAX_REASON;

  const isDisabled = !canEscalate(ticket) || isSubmitting;

  // Submit the escalation request.
  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!isReasonValid || isDisabled) {
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const updated = await ticketsApi.escalate(ticket.id, {
        reason: reason.trim(),
      });

      // Update ticket details without a full page reload.
      onEscalated(updated);

      // Clear the reason after successful escalation.
      setReason('');

      // Refresh escalation history.
      try {
        const updatedHistory = await loadHistory();
        setHistory(updatedHistory);
        setIsLoadingHistory(false);
      } catch {
        // History refresh failure does not undo an escalation.
        setHistory([]);
        setIsLoadingHistory(false);
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Unable to escalate the ticket.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="card">
      <h2>Escalation</h2>

      {!canEscalate(ticket) ? (
        <p className="muted">
          {ticket.priority === 'Critical'
            ? 'Critical tickets cannot be escalated.'
            : 'Resolved or closed tickets cannot be escalated.'}
        </p>
      ) : (
        <form
          onSubmit={handleSubmit}
          className="escalation-form"
        >
          <div className="field">
            <label
              className="field__label"
              htmlFor="escalation-reason"
            >
              Reason{' '}
              <span className="required">
                (5–500 characters)
              </span>
            </label>

            <textarea
              id="escalation-reason"
              rows={3}
              value={reason}
              onChange={(event) =>
                setReason(event.target.value)
              }
              disabled={isSubmitting}
              maxLength={MAX_REASON + 10}
              aria-invalid={
                reasonLength > 0 && !isReasonValid
              }
            />

            <div className="field__hint">
              {reasonLength} / {MAX_REASON}

              {reasonLength > 0 &&
                reasonLength < MIN_REASON && (
                  <span className="field__error">
                    {' '}— too short
                  </span>
                )}
            </div>
          </div>

          {error && (
            <p className="form-error" role="alert">
              {error}
            </p>
          )}

          <button
            type="submit"
            className="button button--primary"
            disabled={!isReasonValid || isSubmitting}
          >
            {isSubmitting
              ? 'Escalating…'
              : 'Escalate ticket'}
          </button>
        </form>
      )}

      <div className="escalation-history">
        <h3>History</h3>

        {isLoadingHistory && (
          <p className="muted">
            Loading history…
          </p>
        )}

        {!isLoadingHistory &&
          history.length === 0 && (
            <p className="muted">
              No escalations yet.
            </p>
          )}

        {!isLoadingHistory &&
          history.length > 0 && (
            <ul className="escalation-list">
              {history.map((item) => (
                <li
                  key={item.id}
                  className="escalation-item"
                >
                  <div className="escalation-item__header">
                    <PriorityBadge
                      priority={item.fromPriority}
                    />

                    <span aria-hidden="true">
                      {' → '}
                    </span>

                    <PriorityBadge
                      priority={item.toPriority}
                    />

                    <span className="escalation-item__when">
                      {formatDateTime(
                        item.escalatedAtUtc
                      )}
                    </span>
                  </div>

                  <p className="escalation-item__reason">
                    {item.reason}
                  </p>

                  <p className="escalation-item__meta">
                    By {item.escalatedBy}

                    {item.fromAgentName !==
                      item.toAgentName && (
                      <>
                        {' · '}
                        Agent:{' '}
                        {item.fromAgentName ??
                          'Unassigned'}
                        {' → '}
                        {item.toAgentName ??
                          'Unassigned'}
                      </>
                    )}
                  </p>
                </li>
              ))}
            </ul>
          )}
      </div>
    </div>
  );
}
