using System.Linq;
using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentQueries agentQueries,
    IUnitOfWork unitOfWork,
    IClock clock,
    TicketTriageService triage,
    GetTicketQueryHandler getTicket,
    ILogger<EscalateTicketCommandHandler> logger)
{
    public async Task<TicketDetailDto> HandleAsync(
        int ticketId,
        EscalateTicketRequest request,
        CancellationToken ct)
    {
        // Validate escalation reason.
        if (string.IsNullOrWhiteSpace(request.Reason) ||
            request.Reason.Trim().Length is < 5 or > 500)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Reason"] = ["Reason must be between 5 and 500 characters."]
            });
        }

        // Validate the person requesting the escalation.
        if (string.IsNullOrWhiteSpace(request.EscalatedBy) ||
            request.EscalatedBy.Trim().Length > 100)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["EscalatedBy"] = ["EscalatedBy is required and must be at most 100 characters."]
            });
        }

        var ticket = await tickets.GetByIdAsync(ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        // BR-7: Critical tickets cannot be escalated further.
        if (ticket.Priority == TicketPriority.Critical)
        {
            throw new ConflictException(
                $"Ticket {ticket.Reference} is already Critical and cannot be escalated.");
        }

        // Resolved and closed tickets cannot be escalated.
        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            throw new ConflictException(
                $"Ticket {ticket.Reference} is {ticket.Status} and cannot be escalated.");
        }

        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);

        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);

        var now = clock.UtcNow;
        var fromPriority = ticket.Priority;
        var fromAgentId = ticket.AssignedAgentId;
        var fromDueAt = ticket.DueAtUtc;

        // Raise the ticket priority by one level.
        var newPriority = fromPriority.RaiseOneLevel();

        // Get agents for triage and reassignment decisions.
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

        // Recalculate the SLA deadline using the new priority.
        // Note: the parameter is named nowUtc in TicketTriageService.
        var decision = triage.Decide(
            requestedPriority: newPriority,
            category: category,
            customer: customer,
            candidates: candidates,
            nowUtc: now);

        // Keep the current agent if they are still eligible.
        // Otherwise, use the agent selected by triage.
        int? newAgentId = fromAgentId;
        if (fromAgentId is not null)
        {
            var stillEligible = candidates.Any(c =>
                c.Id == fromAgentId &&
                c.IsActive &&
                c.OpenTicketCount < c.MaxOpenTickets &&
                (!category.RequiresSpecialist ||
                 c.SpecializationCategoryIds.Contains(category.Id)));

            if (!stillEligible)
            {
                newAgentId = decision.AssignedAgentId;
            }
        }
        else
        {
            newAgentId = decision.AssignedAgentId;
        }

        // Update the ticket aggregate.
        ticket.Escalate(newPriority, decision.DueAtUtc, now);
        ticket.AssignTo(newAgentId, now);

        // Create an immutable escalation history record.
        var escalation = TicketEscalation.Create(
            ticketId: ticket.Id,
            fromPriority: fromPriority,
            toPriority: newPriority,
            fromAgentId: fromAgentId,
            toAgentId: newAgentId,
            fromDueAtUtc: fromDueAt,
            toDueAtUtc: decision.DueAtUtc,
            reason: request.Reason.Trim(),
            escalatedBy: request.EscalatedBy.Trim(),
            escalatedAtUtc: now);

        ticket.RecordEscalation(escalation);

        // Persist the ticket changes and escalation history.
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} escalated from {From} to {To} by {Actor}. Reason: {Reason}",
            ticket.Reference,
            fromPriority,
            newPriority,
            request.EscalatedBy,
            request.Reason);

        // Return the updated ticket details.
        return await getTicket.HandleAsync(ticket.Id, ct);
    }
}