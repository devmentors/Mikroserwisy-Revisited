using Microsoft.Extensions.Logging;
using TicketFlow.CourseUtils;
using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Commands.UnblockTicket;

public class UnblockTicketHandler(ITicketsRepository repository, IMessagePublisher publisher, ILogger<UnblockTicketHandler> logger) 
    : ICommandHandler<UnblockTicket>
{
    public async Task HandleAsync(UnblockTicket command, CancellationToken cancellationToken = default)
    {
        var ticket = await repository.GetAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            throw new TicketFlowException($"Ticket with id {command.TicketId} was not found.");
        }

        ticket.Unblock(command.Reason);
        await repository.UpdateAsync(ticket, cancellationToken);

        await PublishTicketUnblocked(ticket, cancellationToken);
        logger.LogInformation($"Ticket with id {ticket.Id} is now unblocked.");
    }
    
    private async Task PublishTicketUnblocked(Ticket ticket, CancellationToken cancellationToken)
    {
        var ticketStatusChanged = new TicketQualified(ticket.Id, ticket.Version);
        await publisher.PublishAsync(
            message: ticketStatusChanged, 
            routingKey: "ticket-qualified", 
            cancellationToken: cancellationToken);
    }
}