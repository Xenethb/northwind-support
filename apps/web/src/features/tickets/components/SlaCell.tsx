import { Badge, type BadgeTone } from '../../../components/Badge';
import { formatDateTime } from '../../../lib/format';
import type { SlaStatus } from '../../../types/api';

const labels: Record<
  SlaStatus,
  { text: string; tone: BadgeTone; icon: string }
> = {
  WithinSla: { text: 'Within SLA', tone: 'info', icon: '✓' },
  AtRisk: { text: 'At risk', tone: 'warning', icon: '⚠' },
  Breached: { text: 'Breached', tone: 'danger', icon: '✕' },
  Met: { text: 'Met', tone: 'success', icon: '✓' },
  NotApplicable: { text: 'No SLA', tone: 'neutral', icon: '–' },
};

interface SlaIndicatorProps {
  slaStatus: SlaStatus;
  /** Optional due date, shown under the badge when present. */
  dueAtUtc?: string | null;
  /** Compact displays only the badge; full displays the badge and due date. */
  compact?: boolean;
}

/**
 * Reusable SLA indicator for the ticket list and detail page.
 * Each state is conveyed through text and an icon, not colour alone.
 */
export function SlaIndicator({
  slaStatus,
  dueAtUtc,
  compact = false,
}: SlaIndicatorProps) {
  const label = labels[slaStatus];

  return (
    <div className="sla-indicator" data-status={slaStatus}>
      <Badge tone={label.tone}>
        <span aria-hidden="true">{label.icon} </span>
        {label.text}
      </Badge>

      {!compact && dueAtUtc && (
        <span className="sla-indicator__due">
          {formatDateTime(dueAtUtc)}
        </span>
      )}
    </div>
  );
}

/** Backwards-compatible component used by the ticket list table. */
export function SlaCell({
  dueAtUtc,
  slaStatus,
}: {
  dueAtUtc: string | null;
  slaStatus: SlaStatus;
}) {
  return (
    <SlaIndicator
      slaStatus={slaStatus}
      dueAtUtc={dueAtUtc}
    />
  );
}