namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time — never stored.
/// </summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime createdAtUtc,
        DateTime nowUtc,
        int atRiskThresholdPercent = 25)
    {
        if (dueAtUtc is null)
            return SlaStatus.NotApplicable;

        if (resolvedAtUtc is not null)
            return resolvedAtUtc <= dueAtUtc ? SlaStatus.Met : SlaStatus.Breached;

        if (nowUtc > dueAtUtc)
            return SlaStatus.Breached;

        var totalTicks = (dueAtUtc.Value - createdAtUtc).Ticks;
        if (totalTicks <= 0)
            return SlaStatus.WithinSla;

        var remainingPercent = (dueAtUtc.Value - nowUtc).Ticks * 100.0 / totalTicks;

        return remainingPercent <= atRiskThresholdPercent
            ? SlaStatus.AtRisk
            : SlaStatus.WithinSla;
    }
}