using Microsoft.Extensions.Logging;
using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Commands.BlockTicket;

public class BlockTicketHandler(ITicketsRepository repository, IMessagePublisher publisher, ILogger<BlockTicketHandler> logger) 
    : ICommandHandler<BlockTicket>
{
    public async Task HandleAsync(BlockTicket command, CancellationToken cancellationToken = default)
    {
        var ticket = await repository.GetAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            throw new TicketFlowException($"Ticket with id {command.TicketId} was not found.");
        }

        ticket.Block(command.Reason);
        await repository.UpdateAsync(ticket, cancellationToken);
        
        await PublishTicketBlocked(command, cancellationToken, ticket);
        logger.LogInformation($"Ticket with id {ticket.Id} is now blocked.");
    }

    private async Task PublishTicketBlocked(BlockTicket command, CancellationToken cancellationToken, Ticket ticket)
    {
        var ticketStatusChangedMessage = new TicketBlocked(command.TicketId, ticket.Version);
        await publisher.PublishAsync(
            message: ticketStatusChangedMessage, 
            routingKey: "ticket-blocked", 
            cancellationToken: cancellationToken);
    }
}