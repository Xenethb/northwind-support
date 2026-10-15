using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Queries.GetTicketEscalations;

public sealed class GetTicketEscalationsQueryHandler(ITicketRepository tickets)
{
    public async Task<IReadOnlyList<TicketEscalationDto>> HandleAsync(
        int ticketId,
        CancellationToken ct)
    {
        var ticket = await tickets.GetByIdWithEscalationsAsync(ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        return ticket.Escalations
            .OrderByDescending(e => e.EscalatedAtUtc)
            .Select(e => new TicketEscalationDto
            {
                Id = e.Id,
                FromPriority = e.FromPriority,
                ToPriority = e.ToPriority,
                FromAgentId = e.FromAgentId,
                ToAgentId = e.ToAgentId,
                FromDueAtUtc = e.FromDueAtUtc,
                ToDueAtUtc = e.ToDueAtUtc,
                Reason = e.Reason,
                EscalatedBy = e.EscalatedBy,
                EscalatedAtUtc = e.EscalatedAtUtc
            })
            .ToList();
    }
}