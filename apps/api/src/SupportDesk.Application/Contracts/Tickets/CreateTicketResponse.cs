namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// Response returned by POST /api/tickets.
/// Contains the fully populated ticket plus the triage decision reasons.
/// </summary>
public sealed class CreateTicketResponse
{
    public TicketDetailDto Ticket { get; set; } = null!;
    public TriageResultDto Triage { get; set; } = null!;
}