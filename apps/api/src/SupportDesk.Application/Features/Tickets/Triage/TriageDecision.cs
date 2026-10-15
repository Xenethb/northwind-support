using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Triage;

/// <summary>
/// The outcome of automatic triage (priority, due date, assignment and human-readable reasons).
/// Returned by both ticket creation and escalation.
/// </summary>
public sealed record TriageDecision(
    TicketPriority Priority,
    DateTime? DueAtUtc,
    int? AssignedAgentId,
    string PriorityReason,
    string SlaReason,
    string AssignmentReason);