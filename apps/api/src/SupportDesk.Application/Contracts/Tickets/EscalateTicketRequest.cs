namespace SupportDesk.Application.Contracts.Tickets;

public sealed class EscalateTicketRequest
{
    /// <summary>Reason for the escalation (5-500 characters).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Who performed the escalation.
    /// Will be replaced by the authenticated user in Task 3.
    /// </summary>
    public string EscalatedBy { get; set; } = string.Empty;
}