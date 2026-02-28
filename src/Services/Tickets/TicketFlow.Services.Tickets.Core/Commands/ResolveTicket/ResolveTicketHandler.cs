using Microsoft.Extensions.Logging;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Commands.ResolveTicket;

internal sealed class ResolveTicketHandler(ITicketsRepository repository, IMessagePublisher publisher,
    ILogger<ResolveTicketHandler> logger) : ICommandHandler<ResolveTicket>
{
    public async Task HandleAsync(ResolveTicket command, CancellationToken cancellationToken = default)
    {
        var ticket = await repository.GetAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            throw new TicketFlowException($"Ticket with id {command.TicketId} was not found.");
        }

        var oldStatus = ticket.Status.ToString();
        ticket.Resolve(command.Resolution);
        await repository.UpdateAsync(ticket, cancellationToken);

        await PublishTicketResolved(command, cancellationToken, ticket);
        await PublishTicketStatusChanged(ticket, oldStatus, cancellationToken);
        logger.LogInformation($"Ticket with id {ticket.Id} has been resolved.");
    }

    private async Task PublishTicketResolved(ResolveTicket command, CancellationToken cancellationToken, Ticket ticket)
    {
        var ticketStatusChanged = new TicketResolved(command.TicketId, ticket.Version);
        await publisher.PublishAsync(
            message: ticketStatusChanged,
            routingKey: "ticket-resolved",
            cancellationToken: cancellationToken);
    }

    private async Task PublishTicketStatusChanged(Ticket ticket, string oldStatus, CancellationToken cancellationToken)
    {
        var ticketStatusChangedEvent = new TicketFlow.Services.Tickets.Core.Messaging.Publishing.TicketStatusChanged(
            ticket.Id,
            oldStatus,
            ticket.Status.ToString(),
            ticket.AssignedTo);
        await publisher.PublishAsync(
            message: ticketStatusChangedEvent,
            routingKey: "ticket-status-changed",
            cancellationToken: cancellationToken);
    }
}