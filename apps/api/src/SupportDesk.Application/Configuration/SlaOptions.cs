
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Configuration;

/// <summary>
/// Configurable SLA windows and thresholds. Bound from the "Sla" section in appsettings.
/// </summary>
public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public Dictionary<string, int> BaseWindowHours { get; set; } = new();

    public double PremiumCustomerMultiplier { get; set; } = 0.5;

    public int MinimumWindowHours { get; set; } = 1;

    public int AtRiskThresholdPercent { get; set; } = 25;

    public TimeSpan GetBaseWindow(TicketPriority priority)
    {
        var key = priority.ToString();

        if (!BaseWindowHours.TryGetValue(key, out var hours))
            throw new InvalidOperationException(
                $"No SLA base window configured for priority '{key}'.");

        return TimeSpan.FromHours(hours);
    }
}
