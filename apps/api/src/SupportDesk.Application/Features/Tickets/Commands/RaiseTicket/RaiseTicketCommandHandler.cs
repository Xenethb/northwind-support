using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.RaiseTicket;

/// <summary>
/// Raises a ticket for a customer and applies automatic triage (priority, SLA, assignment).
/// </summary>
public sealed class RaiseTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentQueries agentQueries,
    IUnitOfWork unitOfWork,
    IClock clock,
    TicketTriageService triage,
    GetTicketQueryHandler getTicket,
    ILogger<RaiseTicketCommandHandler> logger)
{
    /// <exception cref="NotFoundException">The customer or the category does not exist.</exception>
    public async Task<CreateTicketResponse> HandleAsync(CreateTicketRequest request, CancellationToken ct)
    {
        if (!await customers.ExistsAsync(request.CustomerId, ct))
            throw new NotFoundException("Customer", request.CustomerId);

        if (!await categories.ExistsAsync(request.CategoryId, ct))
            throw new NotFoundException("Category", request.CategoryId);

        var customer = await customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        var category = await categories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException("Category", request.CategoryId);

        // Re-use the existing agent query that already calculates open-ticket counts
        var agentDtos = await agentQueries.GetAllAsync(ct);
        var candidates = agentDtos
            .Select(a => new TicketTriageService.AgentCandidate(
                a.Id,
                a.FullName,
                a.IsActive,
                a.MaxOpenTickets,
                a.OpenTicketCount,
                a.Specializations.Select(s => s.Id).ToList()))
            .ToList();

        var now = clock.UtcNow;

        var decision = triage.Decide(
            request.RequestedPriority,
            category,
            customer,
            candidates,
            now);

        var ticket = Ticket.Raise(
            await tickets.NextReferenceAsync(ct),
            request.Title,
            request.Description,
            request.CustomerId,
            request.CategoryId,
            decision.Priority,
            now);

        ticket.SetDueAt(decision.DueAtUtc, now);

        if (decision.AssignedAgentId is not null)
            ticket.AssignTo(decision.AssignedAgentId, now);

        await tickets.AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} raised. Priority={Priority}, Due={DueAt}, Agent={AgentId}. " +
            "Reasons: {PriorityReason} | {SlaReason} | {AssignmentReason}",
            ticket.Reference,
            decision.Priority,
            decision.DueAtUtc,
            decision.AssignedAgentId,
            decision.PriorityReason,
            decision.SlaReason,
            decision.AssignmentReason);

        var ticketDto = await getTicket.HandleAsync(ticket.Id, ct);

        return new CreateTicketResponse
        {
            Ticket = ticketDto,
            Triage = new TriageResultDto
            {
                PriorityReason = decision.PriorityReason,
                SlaReason = decision.SlaReason,
                AssignmentReason = decision.AssignmentReason
            }
        };
    }
}