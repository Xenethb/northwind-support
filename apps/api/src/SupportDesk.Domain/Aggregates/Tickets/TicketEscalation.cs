namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Immutable record of a single escalation.
/// </summary>
public sealed class TicketEscalation
{
    public int Id { get; private set; }
    public int TicketId { get; private set; }

    public TicketPriority FromPriority { get; private set; }
    public TicketPriority ToPriority { get; private set; }

    public int? FromAgentId { get; private set; }
    public int? ToAgentId { get; private set; }

    public DateTime? FromDueAtUtc { get; private set; }
    public DateTime? ToDueAtUtc { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    public string EscalatedBy { get; private set; } = string.Empty;
    public DateTime EscalatedAtUtc { get; private set; }

    private TicketEscalation() { } // EF

    public static TicketEscalation Create(
        int ticketId,
        TicketPriority fromPriority,
        TicketPriority toPriority,
        int? fromAgentId,
        int? toAgentId,
        DateTime? fromDueAtUtc,
        DateTime? toDueAtUtc,
        string reason,
        string escalatedBy,
        DateTime escalatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length is < 5 or > 500)
            throw new ArgumentException("Reason must be 5-500 characters.", nameof(reason));

        if (string.IsNullOrWhiteSpace(escalatedBy) || escalatedBy.Length > 100)
            throw new ArgumentException("EscalatedBy is required and max 100 characters.", nameof(escalatedBy));

        return new TicketEscalation
        {
            TicketId = ticketId,
            FromPriority = fromPriority,
            ToPriority = toPriority,
            FromAgentId = fromAgentId,
            ToAgentId = toAgentId,
            FromDueAtUtc = fromDueAtUtc,
            ToDueAtUtc = toDueAtUtc,
            Reason = reason.Trim(),
            EscalatedBy = escalatedBy.Trim(),
            EscalatedAtUtc = escalatedAtUtc
        };
    }
}