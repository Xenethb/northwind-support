import { useEffect, useState } from 'react';
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

/** Temporary actor until Task 3 authentication lands. */
const TEMP_ESCALATED_BY = 'Alex Turner';

function canEscalate(ticket: TicketDetail): boolean {
  return (
    ticket.priority !== 'Critical' &&
    ticket.status !== 'Resolved' &&
    ticket.status !== 'Closed'
  );
}

export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [history, setHistory] = useState<TicketEscalation[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(true);

  const loadHistory = async () => {
    setIsLoadingHistory(true);
    try {
      const data = await ticketsApi.getEscalations(ticket.id);
      setHistory(data);
    } catch {
      // History is secondary – don’t block the whole panel
      setHistory([]);
    } finally {
      setIsLoadingHistory(false);
    }
  };

  useEffect(() => {
    void loadHistory();
  }, [ticket.id]);

  const reasonLength = reason.trim().length;
  const isReasonValid = reasonLength >= MIN_REASON && reasonLength <= MAX_REASON;
  const isDisabled = !canEscalate(ticket) || isSubmitting;

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!isReasonValid || isDisabled) return;

    setIsSubmitting(true);
    setError(null);

    try {
      const updated = await ticketsApi.escalate(ticket.id, {
        reason: reason.trim(),
        escalatedBy: TEMP_ESCALATED_BY,
      });
      onEscalated(updated);
      setReason('');
      await loadHistory();
    } catch (err) {
      if (err instanceof ApiError) {
        // 409 Conflict – Critical / Resolved / Closed
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
        <form onSubmit={handleSubmit} className="escalation-form">
          <div className="field">
            <label className="field__label" htmlFor="escalation-reason">
              Reason <span className="required">(5–500 characters)</span>
            </label>
            <textarea
              id="escalation-reason"
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              disabled={isSubmitting}
              maxLength={MAX_REASON + 10} // soft limit
              aria-invalid={reasonLength > 0 && !isReasonValid}
            />
            <div className="field__hint">
              {reasonLength} / {MAX_REASON}
              {reasonLength > 0 && reasonLength < MIN_REASON && (
                <span className="field__error"> — too short</span>
              )}
            </div>
          </div>

          {error && <p className="form-error" role="alert">{error}</p>}

          <button
            type="submit"
            className="button button--primary"
            disabled={!isReasonValid || isSubmitting}
          >
            {isSubmitting ? 'Escalating…' : 'Escalate ticket'}
          </button>
        </form>
      )}

      <div className="escalation-history">
        <h3>History</h3>
        {isLoadingHistory && <p className="muted">Loading history…</p>}
        {!isLoadingHistory && history.length === 0 && (
          <p className="muted">No escalations yet.</p>
        )}
        {!isLoadingHistory && history.length > 0 && (
          <ul className="escalation-list">
            {history.map((item) => (
              <li key={item.id} className="escalation-item">
                <div className="escalation-item__header">
                  <PriorityBadge priority={item.fromPriority} />
                  <span aria-hidden="true"> → </span>
                  <PriorityBadge priority={item.toPriority} />
                  <span className="escalation-item__when">
                    {formatDateTime(item.escalatedAtUtc)}
                  </span>
                </div>
                <p className="escalation-item__reason">{item.reason}</p>
                <p className="escalation-item__meta">
                  By {item.escalatedBy}
                  {item.fromAgentName !== item.toAgentName && (
                    <>
                      {' · '}
                      Agent: {item.fromAgentName ?? 'Unassigned'} →{' '}
                      {item.toAgentName ?? 'Unassigned'}
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