using SupportDesk.Application.Configuration;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Triage;

/// <summary>
/// Pure business rules for priority, SLA window and agent assignment.
/// Shared by ticket creation and escalation so the rules live in one place.
/// </summary>
public sealed class TicketTriageService
{
    private readonly SlaOptions _sla;

    public TicketTriageService(SlaOptions sla)
    {
        _sla = sla;
    }

    /// <summary>
    /// Candidate agent facts needed for assignment (avoids loading full aggregates).
    /// </summary>
    public sealed record AgentCandidate(
        int Id,
        string FullName,
        bool IsActive,
        int MaxOpenTickets,
        int OpenTicketCount,
        IReadOnlyCollection<int> SpecializationCategoryIds);

    public TriageDecision Decide(
        TicketPriority? requestedPriority,
        Category category,
        Customer customer,
        IReadOnlyList<AgentCandidate> candidates,
        DateTime nowUtc)
    {
        // BR-1 Priority
        var priority = requestedPriority ?? TicketPriority.Medium;
        string priorityReason;

        if (category.ForcesCriticalPriority)
        {
            priority = TicketPriority.Critical;
            priorityReason = $"Category '{category.Name}' forces Critical priority.";
        }
        else
        {
            priorityReason = requestedPriority is null
                ? "No priority requested; defaulted to Medium."
                : $"Requested priority {priority} accepted.";
        }

        // BR-2 + BR-3 SLA window
        var baseWindow = _sla.GetBaseWindow(priority);
        var window = baseWindow;

        if (customer.Tier == CustomerTier.Premium)
        {
            window = TimeSpan.FromTicks((long)(baseWindow.Ticks * _sla.PremiumCustomerMultiplier));
            if (window < TimeSpan.FromHours(_sla.MinimumWindowHours))
                window = TimeSpan.FromHours(_sla.MinimumWindowHours);
        }

        var dueAt = nowUtc.Add(window);
        var slaReason = customer.Tier == CustomerTier.Premium
            ? $"Premium customer: {baseWindow.TotalHours}h base × {_sla.PremiumCustomerMultiplier} = {window.TotalHours}h (floor {_sla.MinimumWindowHours}h)."
            : $"Standard customer: {window.TotalHours}h base window.";

        // BR-4 + BR-5 Assignment
        var eligible = candidates
            .Where(a => a.IsActive)
            .Where(a => a.OpenTicketCount < a.MaxOpenTickets)
            .Where(a => !category.RequiresSpecialist ||
                        a.SpecializationCategoryIds.Contains(category.Id))
            .OrderBy(a => a.OpenTicketCount)
            .ThenBy(a => a.Id)          // deterministic tie-break
            .ToList();

        int? agentId = null;
        string assignmentReason;

        if (eligible.Count == 0)
        {
            assignmentReason = category.RequiresSpecialist
                ? $"No active specialist for '{category.Name}' with available capacity."
                : "No active agent with available capacity.";
        }
        else
        {
            var chosen = eligible[0];
            agentId = chosen.Id;
            assignmentReason = $"Assigned to {chosen.FullName} (fewest open tickets among eligible agents).";
        }

        return new TriageDecision(priority, dueAt, agentId, priorityReason, slaReason, assignmentReason);
    }
}